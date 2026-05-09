using Syacapachi.Attribute;
using System.Collections;
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
    [SerializeField] HPManager hpManager;
    [SerializeField] GameObject tutorialLight;
    [SerializeField] PhaseManager phaseManager;
    [SerializeField] PlayerManager PlayerManager;
    [SerializeField] TutorialManager tutorialManager;
    [SerializeField] ResultDataCreater resultDataCreater;
    [Header("Scene Setting")]
    /*
    [SerializeField] SceneAsset homeScene;
    [SerializeField] SceneAsset gameScene;
    [SerializeField] SceneAsset tutorialScene;
    */
    [Header("DataBase")]
    [SerializeField] DifficultyDataBase difficultyRpcDataBase;
    [SerializeField] SceneAsset homeScene;
    [SerializeField] SceneAsset gameScene;
    public GameMode CurrentGameMode => gameMode;
    public HPManager HPManager => hpManager;
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
            if (difficulty.Value == value) return;
            difficulty.Value = value;
        }
    }
    [SerializeField]
    NetworkVariable<GameState> gameState = new(
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

    [Header("SubscribeEvent")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] VoidEvent OnAllPhaseEndedServerEvent;
    [SerializeField] DifficultyEvent difficultyEvent;

    //コルーチンが使用可能なタイミングでは、サーバーかどうかはまだ確定してない。
    private void Start()
    {
        tutorialLight.SetActive(true);
        string sceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(WaitForAllClientConnect(sceneName));
    }
    IEnumerator WaitForAllClientConnect(string sceneName)
    {
        yield return new WaitUntil(() => IsSpawned && PlayerManager != null && PlayerManager.IsAllClientReady());
        if (!IsServer) yield break;
        /*
        if (sceneName.Equals(gameScene.name))
        {
            if (gameStartMode == GameStartMode.Auto)
                StartGameServerOnly();
        }
        else if (sceneName.Equals(tutorialScene.name))
        {
            StartTutorialServerOnly();
        }
        */
        StartTutorialServerOnly();
    }

    private void OnEnable()
    {
        gameState.OnValueChanged += HandleGameStateChanged;
        difficulty.OnValueChanged += HandleDifficltyChange;
    }
    private void OnDisable()
    {
        gameState.OnValueChanged -= HandleGameStateChanged;
        difficulty.OnValueChanged -= HandleDifficltyChange;
    }
    public override void OnNetworkSpawn()
    {
        //初期化。
        difficultyRpcDataBase.CurrectDifficulty = CurrentDifficulty;
        if (!IsServer) return;
        // 🔗 イベント接続
        OnAllPhaseEndedServerEvent.Register(HandleAllPhaseEndedServerOnly);
        OnScoreReachZeroServerEvent.Register(HandleScoreZeroServerOnly);
        difficultyEvent.Register(HandleDifficltyChangeServerOnly);
        //NetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // 🔗 イベント切断
        OnAllPhaseEndedServerEvent.Unregister(HandleAllPhaseEndedServerOnly);
        OnScoreReachZeroServerEvent.Unregister(HandleScoreZeroServerOnly);
        difficultyEvent.Unregister(HandleDifficltyChangeServerOnly);
        //NetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
    }
    void StartTutorialServerOnly()
    {
        tutorialManager.OnTutorialStart();
        gameState.Value = GameState.Tutorial;
    }
    [OnInspectorButton("Start Game")]
    public void StartGameServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Initializing&& CurrentGameState != GameState.Tutorial) return;

        Debug.Log("Game Start", gameObject);
        tutorialLight.SetActive(false);
        //関数内部でデータベースを参照しているので、引数をとらなくても同期されているはず...
        hpManager.SetHPByDifficultyServerOnly(CurrentDifficulty);
        phaseManager.StartPhasesServerOnly(CurrentDifficulty);
        //ここを更新すると、クライアントにイベントが飛ぶ。
        gameState.Value = GameState.Playing;
    }

    [OnInspectorButton("Reset")]
    public void ResetGameServerOnly()
    {
        if (!IsServer) return;

        Debug.Log("GAME RESET", gameObject);

        CurrentGameState = GameState.Initializing;

        phaseManager.ResetPhase();
        phaseManager.KillableHandle.KillAll();
        hpManager.ResetHP();
        foreach (var player in PlayerManager.AllPlayers)
        {
            if (player != null && player.stats != null)
            {
                player.stats.ResetStats();
            }
        }
        MoveScene();
    }
    void HandleDifficltyChangeServerOnly(Difficulty newDifficulty)
    {
        //プレイ中は変更禁止
        if (gameState.Value == GameState.Playing) return;
        CurrentDifficulty = newDifficulty;
    }
    void HandleDifficltyChange(Difficulty oldDifficulty, Difficulty newDifficulty)
    {
        difficultyRpcDataBase.CurrectDifficulty = newDifficulty;
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"GameState Changed: {oldState} -> {newState}", gameObject);
        OnGameStateChangeRpcEvent.Invoke(newState);
        if (newState == GameState.Tutorial || newState == GameState.Playing)
        {
            gunEnableRpcEvent.Invoke(true);
        }
        else
        {
            gunEnableRpcEvent.Invoke(false);
        }
    }
    void HandleScoreZeroServerOnly()
    {
        if (!IsServer) return;
        Debug.Log("GAME OVER");
        CurrentGameState = GameState.GameOver;

        OnGameEnd();
    }

    void HandleAllPhaseEndedServerOnly()
    {
        Debug.Log("ALL PHASE ENDED CALLED");
        CurrentGameState = GameState.GameClear;

        OnGameEnd();
    }
    void OnGameEnd()
    {
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();
        resultDataCreater.CreateAndSendResultData(
            IsGameOver,
            HPManager.GetHP(),
            HPManager.TotalBonusHPServerOnly,
            CurrentDifficulty);
    }
    public void BulletHitProtectArea(int damage)
    {
        hpManager?.AddHPServerOnly(damage);
        InvokeEventRpc();
    }
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void InvokeEventRpc()
    {
        OnbulletComeRpcEvent.Invoke();
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
