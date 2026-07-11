using Syacapachi.Attribute;
using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkGameManager : NetworkBehaviour
{

    [Header("ゲーム設定")]
    [SerializeField] bool useCustomSeed = false;
    [SerializeField,EnableIf(nameof(useCustomSeed))] int gameSeed;
    [SerializeField] NetworkVariable<Difficulty> difficulty = new(Difficulty.Easy);
    [SerializeField] GameMode gameMode = GameMode.Protect;
    [Header("Refernce")]
    [SerializeField] GameObject protectArea;
    [SerializeField] HPManager hpManager;
    [SerializeField] PhaseManager phaseManager;
    [SerializeField] PlayerManager PlayerManager;
    [SerializeField] TutorialManager tutorialManager;
    [SerializeField] ResultDataCreator resultDataCreator;
    [SerializeField] GameStateManager gameStateManager;
    [Header("UIRef")]
    [SerializeField] TMP_Dropdown difficultyDropDown;
    [Header("DataBase")]
    [SerializeField] DifficultyDataBase difficultyRpcDataBase;
    public GameMode CurrentGameMode => gameMode;
    public HPManager HPManager => hpManager;
    public PhaseManager PhaseManager => phaseManager;
    public GameObject ProtectArea => protectArea;

    public bool IsGamePlaying => gameStateManager.IsGamePlaying;
    public bool IsGameOver => gameStateManager.IsGameOver;

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


    [Header("Publish Event")]
    [SerializeField] VoidEvent OnbulletComeRpcEvent;
    [SerializeField] BoolEvent gunEnableRpcEvent;
    [Header("SubscribeEvent")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] VoidEvent OnAllPhaseEndedServerEvent;
    [SerializeField] DifficultyEvent difficultyEvent;
    private void Start()
    {
        gunEnableRpcEvent.Invoke(false);
    }
    private void OnEnable()
    {
        OnGameStateChangeRpcEvent.Register(OnStateChanged);
        difficulty.OnValueChanged += HandleDifficltyChange;
    }
    private void OnDisable()
    {
        OnGameStateChangeRpcEvent.Unregister(OnStateChanged);
        difficulty.OnValueChanged -= HandleDifficltyChange;
    }
    void OnStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Home:
            case GameState.Initializing:
                InitializeGameServerOnly(); break;
            case GameState.Playing:
                StartGameServerOnly();
                break;
            case GameState.Tutorial:
                StartTutorialServerOnly();break;
            default:
                break;
        }
        if(newState == GameState.Playing || newState == GameState.Tutorial)
        {
            gunEnableRpcEvent.Invoke(true);
        }
        else
        {
            gunEnableRpcEvent.Invoke(false);
        }
    }
    public override void OnNetworkSpawn()
    {
        //初期化。
        difficultyRpcDataBase.CurrectDifficulty = CurrentDifficulty;
        //difficultyDropDown.value = (int)CurrentDifficulty;
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
        tutorialManager.OnTutorialStartServerOnly();
    }
    private void StartGameServerOnly()
    {
        Debug.Log("[NetworkGameManager] StartGameServerOnly");
        if (!IsServer) return;
        
        //関数内部でデータベースを参照しているので、引数をとらなくても同期されているはず...
        hpManager.SetHPByDifficultyServerOnly();
        phaseManager.StartPhasesServerOnly(gameSeed);
    }
    /// <summary>
    /// ゲーム初期化
    /// </summary>
    [OnInspectorButton("Initilalzie")]
    private void InitializeGameServerOnly()
    {
        if (!IsServer) return;
        if (!useCustomSeed)
        {
            gameSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
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
    }
    void HandleDifficltyChangeServerOnly(Difficulty newDifficulty)
    {
        //プレイ中は変更禁止
        if (gameStateManager.CurrentGameState == GameState.Playing) return;
        CurrentDifficulty = newDifficulty;
    }
    void HandleDifficltyChange(Difficulty oldDifficulty, Difficulty newDifficulty)
    {
        difficultyRpcDataBase.CurrectDifficulty = newDifficulty;
        difficultyDropDown.value = (int)(newDifficulty);
    }

    
    void HandleScoreZeroServerOnly()
    {
        if (!IsServer) return;
        gameStateManager.OnGameOverServerOnly();

        OnGameEnd();
    }

    void HandleAllPhaseEndedServerOnly()
    {
        gameStateManager.OnGameClearServerOnly(); ;

        OnGameEnd();
    }
    void OnGameEnd()
    {
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();
        resultDataCreator.CreateAndSendResultData(
            new ResultDataCreator.ResultHeaderData(
                IsGameOver,
                gameSeed,
                HPManager.GetHP(),
                HPManager.TotalBonusHPServerOnly,
                CurrentDifficulty
            )
        );
    }
    public void BulletHitProtectArea(int damage)
    {
        hpManager?.AddHPServerOnly(damage);
        InvokeEventRpc();
    }
    [Rpc(SendTo.Server)]
    public void OnDifficultyChangeByUIRpc(int number)
    {
        foreach(var diff in Enum.GetValues(typeof(Difficulty)))
        {
            if(number == (int)diff)
            {
                CurrentDifficulty = (Difficulty)diff;
                break;
            }
        }
    }
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void InvokeEventRpc()
    {
        OnbulletComeRpcEvent.Invoke();
    }
    //void MoveScene()
    //{
    //    if (!IsServer) return;
    //    {
    //        Debug.Log($"[{SceneManager.GetActiveScene().name}] Loading {homeScene}");

    //        NetworkManager.SceneManager.LoadScene(
    //            homeScene,
    //            LoadSceneMode.Single
    //        );
    //    }
    //}
}


public enum GameMode
{
    Protect,   // 拠点防衛あり
    Survival   // 拠点なし（耐久）
}
