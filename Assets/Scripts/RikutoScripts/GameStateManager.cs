using Syacapachi.Attribute;
using System;
using Unity.Netcode;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    enum GameStartMode
    {
        Auto,
        SelectDifficulty
    }
    [Header("ゲームをどのように始めるか")]
    [SerializeField] GameStartMode gameStartMode = GameStartMode.SelectDifficulty;
    [Header("チュートリアルを有効にするか")]
    [SerializeField] bool useTutorial = true;
    [Header("言語選択をUIで行うか")]
    [SerializeField] bool useLanguableSelect = true;
    [Header("世界観説明をするかどうか")]
    [SerializeField] bool showWorldView = false;
    [SerializeField] Language currentLanguage = Language.Japanese;
    [SerializeField]
    NetworkVariable<GameState> gameStateServerWrite = new(
        GameState.Home,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public bool ShowWorldView => showWorldView;
    public GameState CurrentGameState
    {
        get => gameStateServerWrite.Value;
        private set
        {
            TrySetGameState(value);
        }
    }
    LocalState localState = LocalState.LanguageSelect;
    public LocalState LocalState
    {
        get { return localState; }
        private set
        {
            if (TrySetLocalState(value))
            {
                localStateChangeLocalEvent.Invoke(value);
            }
        }
    }
    [Header("Canvas")]
    [SerializeField] GameObject languageCanvas;
    [SerializeField] GameObject connectCanvas;
    [SerializeField] Canvas worldViewCanvas;
    [SerializeField] Collider worldViewCollider;
    [SerializeField] Canvas tutorialUI;
    [SerializeField] GameObject difficultyCanvas;
    [SerializeField] GameObject resetUI;
    [Header("Publish Event")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] LocalStateEvent localStateChangeLocalEvent;
    [SerializeField] DifficultyEvent difficultyEvent;
    [SerializeField] LanguageEvent langageEvent;
    [SerializeField] VoidEvent gameResetEvent;

    public bool IsGamePlaying => CurrentGameState == GameState.Playing || CurrentGameState == GameState.Tutorial;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;
    public Language CurrentLanguage => currentLanguage;

    void Awake()
    {
        LoadLanguageSetting();
        PublishLanguageChanged();
        SetState(LocalState.LanguageSelect);
    }

    public override void OnNetworkSpawn()
    {
        //初期同期
        HandleGameStateChanged(default, CurrentGameState);
        PublishLanguageChanged();
    }
    public override void OnNetworkDespawn()
    {
        //強制初期化
        localState = LocalState.LanguageSelect;
        if (IsServer) 
        {
            gameStateServerWrite.Value = GameState.Home;
        }
        localStateChangeLocalEvent.Invoke(LocalState.LanguageSelect);
    }
    private void OnEnable()
    {
        gameStateServerWrite.OnValueChanged += HandleGameStateChanged;
    }
    private void OnDisable()
    {
        gameStateServerWrite.OnValueChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        OnGameStateChangeRpcEvent.Invoke(newState);

        switch (newState)
        {
            case GameState.Home:
                LocalState = 
                    localState == LocalState.NetworkConnect
                    ? LocalState.WorldView
                    : LocalState.LanguageSelect;
                break;

            case GameState.SelecrDifficulty:
                LocalState = LocalState.SelectDifficulty;
                break;

            case GameState.Tutorial:
                LocalState = LocalState.Tutorial;
                break;

            case GameState.Playing:
                LocalState = LocalState.Playing;
                break;

            case GameState.GameClear:
            case GameState.GameOver:
                LocalState = LocalState.End;
                break;
        }
    }
    private void SetState(LocalState state)
    {
        //言語選択をしない場合スキップ
        if(state == LocalState.LanguageSelect && !useLanguableSelect)
        {
            OnLangageDefined();
            return;
        }
        languageCanvas.SetActive(
            state == LocalState.LanguageSelect);

        connectCanvas.SetActive(
            state == LocalState.NetworkConnect ||
            state == LocalState.WorldView);

        bool worldViewEnable = state == LocalState.WorldView;
        worldViewCanvas.enabled = worldViewEnable;
        worldViewCollider.enabled = worldViewEnable;

        tutorialUI.enabled =
            state == LocalState.Tutorial;

        difficultyCanvas.SetActive(
            state == LocalState.SelectDifficulty);

        resetUI.SetActive( 
            state == LocalState.End);
    }


    /// <summary>
    /// UI状態の変化。
    /// とりあえず、全部許可。
    /// </summary>
    /// <param name="fromState"></param>
    /// <param name="toState"></param>
    /// <returns></returns>
    private static bool CanTransition(LocalState fromState, LocalState toState)
    {
        return true;
    }

    private static bool CanTransition(GameState fromState, GameState toState, bool isSpawned)
    {
        //切断されたらHomeへ行く。
        if (!isSpawned && toState == GameState.Home) return true;
        // 展示用リセット
        return fromState switch
        {
            GameState.Home
                => toState == GameState.Initializing,

            GameState.Initializing
                => toState == GameState.SelecrDifficulty
                || toState == GameState.Tutorial
                || toState == GameState.Playing
                || toState == GameState.Home,

            GameState.Tutorial
                => toState == GameState.Playing,

            GameState.SelecrDifficulty
                => toState == GameState.Tutorial
                || toState == GameState.Playing
                || toState == GameState.Home,

            GameState.Playing
                => toState == GameState.GameClear
                || toState == GameState.GameOver,

            GameState.GameClear or GameState.GameOver
                => toState == GameState.Home,

            _ => false,
        };
    }

    private bool TrySetGameState(GameState nextState)
    {
        if (!IsServer) return false;

        GameState currentState = gameStateServerWrite.Value;
        if (currentState == nextState) return true;

        if (!CanTransition(currentState, nextState, IsSpawned))
        {
            Debug.LogWarning(
                $"[{nameof(GameStateManager)}] Invalid GameState transition: {currentState} -> {nextState}",
                gameObject);
            return false;
        }

        gameStateServerWrite.Value = nextState;
        return true;
    }

    private bool TrySetLocalState(LocalState nextState)
    {
        LocalState currentState = localState;
        if (currentState == nextState) return true;

        if (!CanTransition(currentState, nextState))
        {
            Debug.LogWarning(
                $"[{nameof(GameStateManager)}] Invalid LocalState transition: {currentState} -> {nextState}",
                gameObject);
            return false;
        }

        localState = nextState;
        SetState(localState);
        return true;
    }

    public void OnTutorialEndServerOnly()
    {
        if (!IsServer) return;

        CurrentGameState = GameState.Playing;
    }



    private void LoadLanguageSetting()
    {
        int savedLanguage = PlayerPrefs.GetInt(nameof(Language), (int)Language.Japanese);
        currentLanguage = Enum.IsDefined(typeof(Language), savedLanguage)
            ? (Language)savedLanguage
            : Language.Japanese;
    }

    private void SaveLanguageSetting()
    {
        PlayerPrefs.SetInt(nameof(Language), (int)currentLanguage);
        PlayerPrefs.Save();
    }

    private void PublishLanguageChanged()
    {
        langageEvent.Invoke(currentLanguage);
    }
    [OnInspectorButton(drawOrder: 4,validateInvoke: true)]
    public void ChangeLanguage(Language language)
    {
        currentLanguage = language;
        SaveLanguageSetting();
        PublishLanguageChanged();
    }

    void StartGame(Difficulty difficulty)
    {
        if (!IsServer) return;

        if (CurrentGameState != GameState.SelecrDifficulty)
            return;

        difficultyEvent.Invoke(difficulty);

        Debug.Log($"Start Difficulty : {difficulty}");

        CurrentGameState = useTutorial
            ? GameState.Tutorial
            : GameState.Playing;
    }

    [Rpc(SendTo.Server)]
    void RequestStartGameRpc(Difficulty difficulty)
    {
        StartGame(difficulty);
    }
    public void OnGameOverServerOnly()
    {
        if (!IsServer) return;
        CurrentGameState = GameState.GameOver;
    }
    public void OnGameClearServerOnly()
    {
        if (!IsServer) return;
        CurrentGameState = GameState.GameClear;
    }
    public void OnGameInitializeServerOnly()
    {
        if (!IsServer) return;

        CurrentGameState = GameState.Initializing;

        if (gameStartMode == GameStartMode.SelectDifficulty)
        {
            CurrentGameState = GameState.SelecrDifficulty;
        }
        else if (useTutorial)
        {
            CurrentGameState = GameState.Tutorial;
        }
        else
        {
            CurrentGameState = GameState.Playing;
        }
    }
    public void OnGameStartServerOnly()
    {
        if (!IsServer) return;
        CurrentGameState = GameState.Playing;
    }
    public void OnBackToHomeServerOnly()
    {
        if (!IsServer) return;
        CurrentGameState = GameState.Home;
        NotifyResetRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void NotifyResetRpc()
    {
        gameResetEvent.Invoke();
    }

    public void EnterLanguageSelect()
    {
        LocalState = LocalState.LanguageSelect;
    }

    public void OnLangageDefined()
    {
        if (!IsSpawned)
        {
            LocalState = LocalState.NetworkConnect;
        }
        else
        {
            OnConnection();
        }
    }
    public void OnConnection()
    {
        LocalState = LocalState.WorldView;
    }
#if UNITY_EDITOR
    [OnInspectorButton(drawOrder: 0)]
    private void StartEasy()
    {
        RequestStartGameRpc(Difficulty.Easy);
    }

    [OnInspectorButton(drawOrder: 1)]
    private void StartNormal()
    {
        RequestStartGameRpc(Difficulty.Normal);
    }

    [OnInspectorButton(drawOrder: 2)]
    private void StartHard()
    {
        RequestStartGameRpc(Difficulty.Hard);
    }

    [OnInspectorButton(drawOrder: 3)]
    private void StartDebug()
    {
        RequestStartGameRpc(Difficulty.Debug);
    }
    [OnInspectorButton("SetStateServerOnly", drawOrder: 4)]
    [Rpc(SendTo.Server)]
    private void SetStateRpc(GameState newState)
    {
        gameStateServerWrite.Value = newState;
    }
    [OnInspectorButton(drawOrder: 4, showOnlyInPlayMode: true)]
    private void SetStateRequest(GameState newState)
    {
        SetStateRpc(newState);
    }

    [OnInspectorButton("Reset Game ServerOnly",drawOrder: 100)]
    private void ResetGame()
    {
        RequestResetGameRpc();
    }
    [OnInspectorButton(drawOrder: 101,showOnlyInPlayMode:true)]
    [Rpc(SendTo.Server)]
    private void RequestResetGameRpc()
    {
        gameStateServerWrite.Value = GameState.Home;
        NotifyResetRpc();
    }
#endif
}
public enum GameState
{
    /// <summary>
    /// ゲーム内部の初期化、チュートリアルへ遷移するかが分岐、まだ始まってない
    /// </summary>
    Initializing,
    /// <summary>
    /// プレイ開始時
    /// </summary>
    Playing,
    /// <summary>
    /// ゲームクリア
    /// </summary>
    GameClear,
    /// <summary>
    /// ゲームオーバー
    /// </summary>
    GameOver,
    /// <summary>
    /// チュートリアル開始時
    /// </summary>
    Tutorial,
    /// <summary>
    /// 待機WorldViewとかにいる時
    /// </summary>
    Home,
    /// <summary>
    /// ゲーム開始前の難易度選択
    /// </summary>
    SelecrDifficulty,
}
//[GenerateEvent(typeof(GameEventSOBase<>))]
public enum LocalState
{
    LanguageSelect,//言語設定
    NetworkConnect,//接続
    WorldView,//世界観説明
    Tutorial,//チュートリアル
    SelectDifficulty,//難易度選択
    Playing,//ゲーム開始
    End,//ゲーム終了
}
//LocalStateの動き
//Lang -> (connect) -> worldView -> Playing ->GameState
//Lang -> connect : 言語選択後、ネット接続してない場合遷移
//Lang -> worldView :言語選択後、ネット接続している場合遷移
//connect -> worldView:ネット接続した場合遷移
//worlldView -> Playing:クライアント全員がworldViewの時、進むを押したとき。
//Any -> Langネット接続が切れたとき。


//GameState
//Initialize -> (SelecrDifficulty) -> (Tutorial) -> Playing -> {GameClear, GameOver} -> Home;
//Initialize(難易度選択/チュートリアル/プレイ開始の分岐を判断する枝)ついでに初期化
//Initialize -> SelecrDifficulty: buttonモードの場合遷移
//SelecrDifficulty -> Tutorial:チュートリアルが有効の場合遷移
//SelecrDifficulty -> Playing:チュートリアルが無効の場合遷移
//Tutorial -> Playing:チュートリアル終了後遷移
//PLaying -> {GameClear, GameOver} :条件により、どちらかに遷移
//{GameClear, GameOver} -> Home : 戻るボタンで遷移
