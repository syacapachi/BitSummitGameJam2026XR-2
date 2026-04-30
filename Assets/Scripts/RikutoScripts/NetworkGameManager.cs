using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGameManager : NetworkBehaviour
{
    enum GameStartMode
    {
        Auto,
        Button
    }
    [Header("ゲーム設定")]
    [SerializeField] NetworkVariable<Difficulty> difficulty = new(Difficulty.Easy);
    [SerializeField] GameMode gameMode = GameMode.Protect;
    [SerializeField] GameStartMode gameStartMode = GameStartMode.Button;
    [Header("Refernce")]
    [SerializeField] GameObject protectArea;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PhaseManager phaseManager;
    [SerializeField] PlayerManager PlayerManager;
    
    public GameMode CurrentGameMode => gameMode;
    public ScoreManager ScoreManager => scoreManager;
    public PhaseManager PhaseManager => phaseManager;
    public GameObject ProtectArea => protectArea;
    public bool IsGamePlaying => CurrentGameState == GameState.Playing;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;
    
    public Difficulty CurrentDifficulty
    {
        get => difficulty.Value;
        private set
        {
            if (!IsServer) return;
            if (difficulty.Value != value)
            {
                difficulty.Value = value;
            }
        }
    }
    [SerializeField] NetworkVariable<GameState> gameState = new(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public GameState CurrentGameState
    {
        get => gameState.Value;
        private set
        {
            if (!IsServer) return;
            if (gameState.Value == value) return;
            gameState.Value = value;
        }
    }

    [Header("Publish Event")]
    [SerializeField] VoidEvent OnbulletComeRpcEvent;
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] PlayerResultDataArrayEvent RecieveResultRpcEvent;

    [Header("SubscribeEvent")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] VoidEvent OnAllPhaseEndedServerEvent;
    [SerializeField] DifficultyEvent difficultyEvent;

    //コルーチンが使用可能なタイミングでは、サーバーかどうかはまだ確定してない。
    private void Start()
    {
        if(gameStartMode == GameStartMode.Auto)
        {
            StartCoroutine(WaitForLoad());
        }
    }

    private void OnEnable()
    {
        gameState.OnValueChanged += HandleGameStateChanged;
        
    }
    private void OnDisable()
    {
        gameState.OnValueChanged -= HandleGameStateChanged;
        
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // 🔗 イベント接続
        OnAllPhaseEndedServerEvent.Register(HandleAllPhaseEnded);
        OnScoreReachZeroServerEvent.Register(HandleScoreZeroServer);
        difficultyEvent.Register(HandleDifficltyChange);
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // 🔗 イベント切断
        OnAllPhaseEndedServerEvent.Unregister(HandleAllPhaseEnded);
        OnScoreReachZeroServerEvent.Unregister(HandleScoreZeroServer);
        difficultyEvent.Unregister(HandleDifficltyChange);
    }

    IEnumerator WaitForLoad()
    {
        yield return new WaitUntil(() => IsSpawned && PlayerManager != null && PlayerManager.IsAllClientReady());
        if (IsServer)
        {
            StartGameServerOnly();
        }
    }

    [OnInspectorButton("Start Game")]
    public void StartGameServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Initializing) return;

        Debug.Log("Game Start");
        scoreManager.SetScoreServerOnly();
        phaseManager.StartPhasesRpc(CurrentDifficulty);
        gameState.Value = GameState.Playing;
    }

    [OnInspectorButton("Reset")]
    public void ResetGameServerOnly()
    {
        if (!IsServer) return;

        Debug.Log("GAME RESET");

        CurrentGameState = GameState.Initializing;

        phaseManager.ResetPhase();
        phaseManager.KillableHandle.KillAll();
        scoreManager.ResetScore();
        MoveScene();
    }
    void HandleDifficltyChange(Difficulty newDifficulty)
    {
        CurrentDifficulty = newDifficulty;
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        OnGameStateChangeRpcEvent.Invoke(newState);
    }
    void HandleScoreZeroServer()
    {
        if (!IsServer) return;
        Debug.Log("GAME OVER");
        CurrentGameState = GameState.GameOver;
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();

        SendResults();
    }

    void HandleAllPhaseEnded()
    {
        Debug.Log("GAME CLEAR");
        CurrentGameState = GameState.GameClear;
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();

        SendResults();
    }

    public void BulletHitProtectArea(int damage)
    {
        scoreManager?.AddScoreServerOnly(damage);
        InvokeEventRpc();
    }
    [Rpc(SendTo.Everyone,InvokePermission = RpcInvokePermission.Server)]
    private void InvokeEventRpc()
    {
        OnbulletComeRpcEvent.Invoke();
    }


    [Rpc(SendTo.ClientsAndHost)]
    void OnSendResultRpc(PlayerResultData[] result)
    {
        RecieveResultRpcEvent.Invoke(result);
    }
    [OnInspectorButton]
    void SendMockData(PlayerResultData[] data)
    {
        OnSendResultRpc(data);
    }
    void SendResults()
    {
        var list = new List<PlayerResultData>();

        foreach (var player in PlayerManager.AllPlayers)
        {
            if (player == null) continue;

            var stats = player.stats;
            if (stats == null) continue;

            list.Add(stats.CreateResultDataServerOnly());
        }

        OnSendResultRpc(list.ToArray());
    }

    void MoveScene()
    {
        if (!IsServer) return;
        {
            Debug.Log("[VRSystemScene] Loading WorldViewScene");

            NetworkManager.SceneManager.LoadScene(
                "WorldViewScene",
                LoadSceneMode.Single
            );
        }
    }
}
public enum GameState
{
    Initializing,// 開始前
    Playing,     // プレイ中
    GameClear,   // クリア
    GameOver     // ゲームオーバー
}

public enum GameMode
{
    Protect,   // 拠点防衛あり
    Survival   // 拠点なし（耐久）
}
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Debug
}