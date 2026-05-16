using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameStateManager : NetworkBehaviour
{
    enum GameStartMode
    {
        Auto,
        Button
    }
    [SerializeField] GameStartMode gameStartMode = GameStartMode.Button;
    [SerializeField] bool useTutorial = true;
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
    [SerializeField] Canvas tutorialUI;
    [SerializeField] Canvas difficultyCanvas;
    [Header("Publish Event")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] LocalStateEvent localStateChangeLocalEvent;
    [SerializeField] DifficultyEvent difficultyEvent;

    public bool IsGamePlaying => CurrentGameState == GameState.Playing || CurrentGameState == GameState.Tutorial;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;

    void Awake()
    {
        SetState(LocalState.LanguageSelect);
    }

    public override void OnNetworkSpawn()
    {
        if (localState == LocalState.NetworkConnect)
        {
            LocalState = LocalState.WorldView;
        }
        //初期同期
        OnGameStateChangeRpcEvent.Invoke(CurrentGameState);
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
                LocalState = LocalState.LanguageSelect;
                break;

            case GameState.Waiting:
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

        worldViewCanvas.enabled =
            state == LocalState.WorldView;

        tutorialUI.enabled =
            state == LocalState.Tutorial;

        difficultyCanvas.enabled =
            state == LocalState.SelectDifficulty;
    }


    /*
    private bool CanTransition(GameState fromState, GameState toState)
    {
        return fromState switch
        {
            GameState.Home => toState == GameState.Initializing,
            GameState.Initializing => toState == GameState.Tutorial || toState == GameState.Playing,
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

    private bool CanTransition(LocalState fromState, LocalState toState)
    {
        return fromState switch
        {
            LocalState.LanguageSelect
                => IsSpawned
                ? toState == LocalState.WorldView
                : toState == LocalState.NetworkConnect,

            LocalState.NetworkConnect
                => toState == LocalState.WorldView,

            LocalState.WorldView
                => toState == LocalState.Tutorial
                || toState == LocalState.SelectDifficulty
                || toState == LocalState.Playing,

            LocalState.Tutorial
                => toState == LocalState.SelectDifficulty
                || toState == LocalState.Playing,

            LocalState.SelectDifficulty
                => toState == LocalState.Playing,

            LocalState.Playing
                => toState == LocalState.LanguageSelect,

            _ => false,
        };
    }

    private bool CanTransition(GameState fromState, GameState toState)
    {
        return fromState switch
        {
            GameState.Home
                => toState == GameState.Initializing,

            GameState.Initializing
                => toState == GameState.Tutorial
                || toState == GameState.Playing
                || toState == GameState.Waiting,

            GameState.Tutorial
                => toState == GameState.Waiting
                || toState == GameState.Playing,

            GameState.Waiting
                => toState == GameState.Playing,

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

        if (gameStartMode == GameStartMode.Button)
        {
            CurrentGameState = GameState.Waiting;
        }
        else
        {
            CurrentGameState = GameState.Playing;
        }
    }

    [OnInspectorButton]
    public void StartEasy()
    {
        StartGame(Difficulty.Easy);
    }

    [OnInspectorButton]
    public void StartNormal()
    {
        StartGame(Difficulty.Normal);
    }

    [OnInspectorButton]
    public void StartHard()
    {
        StartGame(Difficulty.Hard);
    }

    void StartGame(Difficulty difficulty)
    {
        if (!IsServer) return;

        if (CurrentGameState != GameState.Waiting)
            return;

        difficultyEvent.Invoke(difficulty);

        Debug.Log($"Start Difficulty : {difficulty}");

        CurrentGameState = GameState.Playing;
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

        if (useTutorial)
        {
            CurrentGameState = GameState.Tutorial;
        }
        else
        {
            if (gameStartMode == GameStartMode.Auto)
            {
                CurrentGameState = GameState.Playing;
            }
            else
            {
                CurrentGameState = GameState.Waiting;
            }
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
    /// ゲーム開始前の待機状態
    /// </summary>
    Waiting,
}
[GenerateEvent(typeof(GameEventSOBase<>))]
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


//GameState
//Initialize -> (tutorial) -> (Waiting) -> Playing -> {GameClear, GameOver} -> Home;
//Initialize(チュートリアルの分岐を判断する枝)ついでに初期化
//Initialize -> Tutorial:チュートリアルが有効の場合遷移
//Initialize -> Playing:チュートリアルが無効の場合遷移
//Tutorial -> Plying:チュートリアル終了後遷移
//PLaying -> {GameClear, GameOver} :条件により、どちらかに遷移
//{GameClear, GameOver} -> Home : 戻るボタンで遷移

//Waiting追加　buttonモードのとき難易度選択する状態
