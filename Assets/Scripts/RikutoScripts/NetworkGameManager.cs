using Syacapachi.Attribute;
using Syacapachi.Data;
using Syacapachi.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
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
    [SerializeField] TutorialManager tutorialManager;
    [Header("Back Scene")]
    [SerializeField] SceneAsset homeScene;
    
    public GameMode CurrentGameMode => gameMode;
    public ScoreManager ScoreManager => scoreManager;
    public PhaseManager PhaseManager => phaseManager;
    public GameObject ProtectArea => protectArea;
    public bool IsGamePlaying => CurrentGameState == GameState.Playing || CurrentGameState == GameState.Tutorial;
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
    [SerializeField] BoolEvent gunEnableRpcEvent;
    [SerializeField] ResultDataEvent resultDataRpcEvent;

    [Header("SubscribeEvent")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] VoidEvent OnAllPhaseEndedServerEvent;
    [SerializeField] DifficultyEvent difficultyEvent;

    //コルーチンが使用可能なタイミングでは、サーバーかどうかはまだ確定してない。
    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(WaitForAllClientConnect(sceneName));
    }
    IEnumerator WaitForAllClientConnect(string sceneName)
    {
        yield return new WaitUntil(() => IsSpawned && PlayerManager != null && PlayerManager.IsAllClientReady());
        if (!IsServer) yield break;
        if (sceneName.Equals("VRSystemScene"))
        {
            if (gameStartMode == GameStartMode.Auto)
                StartGameServerOnly();
        }
        else if (sceneName.Equals("TutorialScene"))
        {
            StartTutorialServerOnly();
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
        //NetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // 🔗 イベント切断
        OnAllPhaseEndedServerEvent.Unregister(HandleAllPhaseEnded);
        OnScoreReachZeroServerEvent.Unregister(HandleScoreZeroServer);
        difficultyEvent.Unregister(HandleDifficltyChange);
        //NetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
    }
    //private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    //{
    //    if (sceneName.Equals("TutorialScene"))
    //    {
    //        StartTutorial();
    //    }
    //}
    void StartTutorialServerOnly()
    {
        tutorialManager.OnTutorialStart();
        gameState.Value = GameState.Tutorial;
    }
    [OnInspectorButton("Start Game")]
    public void StartGameServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Initializing) return;

        Debug.Log("Game Start");
        scoreManager.SetScoreByDifficultyServerOnly(CurrentDifficulty);
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
        foreach (var player in PlayerManager.AllPlayers)
        {
            if (player != null && player.stats != null)
            {
                player.stats.ResetStats();
            }
        }
        MoveScene();
    }
    void HandleDifficltyChange(Difficulty newDifficulty)
    {
        CurrentDifficulty = newDifficulty;
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"GameState Changed: {oldState} -> {newState}");
        OnGameStateChangeRpcEvent.Invoke(newState);
        if(newState == GameState.Tutorial || newState == GameState.Playing)
        {
            gunEnableRpcEvent.Invoke(true);
        }
        else
        {
            gunEnableRpcEvent.Invoke(false);
        }
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
        Debug.Log("ALL PHASE ENDED CALLED");
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
    void OnSendResultRpc(ResultData result)
    {
        resultDataRpcEvent.Invoke(result);
        Debug.Log($"[{nameof(NetworkGameManager)}] {gameObject.name} Recived Data \n Detail = {JsonUtility.ToJson(result, true)}", gameObject);
    }
    void SendResults()
    {
        var list = new List<PlayerResultData>();
        Debug.Log($"AllPlayers Count = {PlayerManager.AllPlayers.Count}");

        foreach (var player in PlayerManager.AllPlayers)
        {
            if (player == null) continue;

            var stats = player.stats;
            if (stats == null) continue;

            list.Add(stats.CreateResultDataServerOnly());
        }
        PlayerResultData[] datas = list.ToArray();
        float cooporate = ResultUI.CalculateCooperation(datas);
        ResultData data = new ResultData()
        {
            Time = DateTime.Now.ToString(),
            TotalScore = ScoreManager.GetScore(),
            TotalBonus = ScoreManager.TotalBonus,
            Cooperation = cooporate,
            IsGameOver = IsGameOver,
            GameSeed = -1,
            detail = datas
        };
        OnSendResultRpc(data);
    }

    void MoveScene()
    {
        if (!IsServer) return;
        {
            Debug.Log($"[{SceneManager.GetActiveScene().name}] Loading {homeScene.name}");

            NetworkManager.SceneManager.LoadScene(
                homeScene.name,
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
    GameOver,    // ゲームオーバー
    Tutorial     //チュートリアル(統合を見据えて置いておく)
}

public enum GameMode
{
    Protect,   // 拠点防衛あり
    Survival   // 拠点なし（耐久）
}
