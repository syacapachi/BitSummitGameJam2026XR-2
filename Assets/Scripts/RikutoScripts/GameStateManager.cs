using Syacapachi.Attribute;
using System;
using Unity.Netcode;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    enum GameStartMode
    {
        Auto,
        Button
    }
    [SerializeField] GameStartMode gameStartMode = GameStartMode.Button;
    [SerializeField] bool useTutorial = true;
    [SerializeField] bool showWorldView = false;
    [SerializeField] Language currentLanguage = Language.Japanese;
    [SerializeField]
    NetworkVariable<GameState> gameState = new(
        GameState.Home,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public GameState CurrentGameState
    {
        get => gameState.Value;
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
    [Header("Publish Event")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] LocalStateEvent localStateChangeLocalEvent;
    [SerializeField] DifficultyEvent difficultyEvent;
    [SerializeField] LanguageEvent langageEvent;

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
        gameState.Value = GameState.Home;
        localStateChangeLocalEvent.Invoke(LocalState.LanguageSelect);
    }
    private void OnEnable()
    {
        gameState.OnValueChanged += HandleGameStateChanged;
    }
    private void OnDisable()
    {
        gameState.OnValueChanged -= HandleGameStateChanged;
    }
    /*
    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        OnGameStateChangeRpcEvent.Invoke(newState);
        if(newState == GameState.Home)
        {
            LocalState = LocalState.LanguageSelect;
        }
        else
        {
            LocalState = LocalState.Playing;
        }

        UpdateTutorialUI();
    }
    */

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
        }
    }
    /*
    private void SetState(LocalState state)
    {
        languageCanvas.SetActive(
            state == LocalState.LanguageSelect);

        connectCanvas.SetActive(
            state == LocalState.NetworkConnect ||
            state == LocalState.WorldView);

        worldViewCanvas.enabled =
            state == LocalState.WorldView;

        UpdateTutorialUI();
    }
    */

    private void SetState(LocalState state)
    {
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
    }


    /*
    private bool CanTransition(GameState fromState, GameState toState)
    {
        return fromState switch
        {
            GameState.Home => toState == GameState.Initializing,
            GameState.Initializing => toState == GameState.SelecrDifficulty || toState == GameState.Tutorial || toState == GameState.Playing,
            GameState.SelecrDifficulty => toState == GameState.Tutorial || toState == GameState.Playing,
            GameState.Tutorial => toState == GameState.Playing,
            GameState.Playing => toState == GameState.GameClear || toState == GameState.GameOver,
            GameState.GameClear or GameState.GameOver => toState == GameState.Home,
            _ => false,
        };
    }

    private bool CanTransition(LocalState fromState, LocalState toState)
    {
        return fromState switch
        {
            LocalState.LanguageSelect => IsSpawned ? toState == LocalState.WorldView : toState == LocalState.NetworkConnect,
            LocalState.NetworkConnect => toState == LocalState.WorldView,
            LocalState.WorldView => toState == LocalState.Playing,
            LocalState.Playing => toState == LocalState.LanguageSelect,
            _ => false,
        };
    }
    */
    /// <summary>
    /// UI状態の変化。
    /// とりあえず、全部許可。
    /// </summary>
    /// <param name="fromState"></param>
    /// <param name="toState"></param>
    /// <returns></returns>
    private bool CanTransition(LocalState fromState, LocalState toState)
    {
        return true;
    }

    private bool CanTransition(GameState fromState, GameState toState)
    {
        //切断されたらHomeへ行く。
        if (!IsSpawned && toState == GameState.Home) return true;
        return fromState switch
        {
            GameState.Home
                => toState == GameState.Initializing,

            GameState.Initializing
                => toState == GameState.SelecrDifficulty
                || toState == GameState.Tutorial
                || toState == GameState.Playing,

            GameState.Tutorial
                => toState == GameState.Playing,

            GameState.SelecrDifficulty
                => toState == GameState.Tutorial
                || toState == GameState.Playing,

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

        GameState currentState = gameState.Value;
        if (currentState == nextState) return true;

        if (!CanTransition(currentState, nextState))
        {
            Debug.LogWarning(
                $"[{nameof(GameStateManager)}] Invalid GameState transition: {currentState} -> {nextState}",
                gameObject);
            return false;
        }

        gameState.Value = nextState;
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

    [OnInspectorButton(drawOrder: 3)]
    public void StartEasy()
    {
        RequestStartGameRpc(Difficulty.Easy);
    }

    [OnInspectorButton(drawOrder: 2)]
    public void StartNormal()
    {
        RequestStartGameRpc(Difficulty.Normal);
    }

    [OnInspectorButton(drawOrder: 1)]
    public void StartHard()
    {
        RequestStartGameRpc(Difficulty.Hard);
    }

    [OnInspectorButton(drawOrder: 0)]
    public void StartDebug()
    {
        RequestStartGameRpc(Difficulty.Debug);
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
        langageEvent?.Invoke(currentLanguage);
    }
    [OnInspectorButton(drawOrder: 6)]
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

        if (gameStartMode == GameStartMode.Button)
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
