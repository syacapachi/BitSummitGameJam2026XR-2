using UnityEngine;
using Unity.Netcode;

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
            if (!IsServer) return;
            if (gameState.Value == value) return;
            gameState.Value = value;
        }
    }
    LocalState localState = LocalState.LanguageSelect;
    public LocalState LocalState
    {
        get { return localState; }
        private set
        {
            localState = value;
            SetState(localState);
        }
    }
    [Header("Canvas")]
    [SerializeField] GameObject languageCanvas;
    [SerializeField] GameObject connectCanvas;
    [SerializeField] Canvas worldViewCanvas;
    [Header("Publish Event")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;

    public bool IsGamePlaying => CurrentGameState == GameState.Playing || CurrentGameState == GameState.Tutorial;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;

    void Awake()
    {
        SetState(LocalState.LanguageSelect);
    }
    private void OnEnable()
    {
        gameState.OnValueChanged += HandleGameStateChanged;
    }
    private void OnDisable()
    {
        gameState.OnValueChanged -= HandleGameStateChanged;
    }
    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"GameState Changed: {oldState} -> {newState}", gameObject);
        OnGameStateChangeRpcEvent.Invoke(newState);
    }

    private void SetState(LocalState state)
    {

        languageCanvas.SetActive(
            state == LocalState.LanguageSelect);

        connectCanvas.SetActive(
            state == LocalState.NetworkConnect ||
            state == LocalState.WorldView);

        worldViewCanvas.enabled =
            state == LocalState.WorldView;
    }
    public void OnTutorialEnd()
    {
        if (CurrentGameState != GameState.Tutorial) return;
        if (gameStartMode == GameStartMode.Auto)
        {
            CurrentGameState = GameState.Playing;
        }
    }
    public void OnGameOverServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Playing) return;
        CurrentGameState = GameState.GameOver;
    }
    public void OnGameClearServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Playing) return;
        CurrentGameState = GameState.GameClear;
    }
    public void OnGameInitialize()
    {
        LocalState = LocalState.Playing;
        //ここからサーバー
        if (!IsServer) return;
        CurrentGameState = GameState.Initializing;
        if (useTutorial)
        {
            CurrentGameState = GameState.Tutorial;
        }
        else if(gameStartMode == GameStartMode.Auto)
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

    public void EnterNetworkConnect()
    {
        if (!IsSpawned)
        {
            LocalState = LocalState.NetworkConnect;
        }
        else
        {
            EnterWorldView();
        }
    }
    public void EnterWorldView()
    {
        LocalState = LocalState.WorldView;
    }
}
public enum GameState
{
    Initializing,// 初期化
    Playing,     // プレイ中
    GameClear,   // クリア
    GameOver,    // ゲームオーバー
    Tutorial,    //チュートリアル
    Home         //待機
}
public enum LocalState
{
    LanguageSelect,//言語設定
    NetworkConnect,//接続
    WorldView,//世界観説明
    Playing,//ゲーム開始
}
//Stateの動き
//Lang -> (connect) -> worldView -> Playing
//Initialize -> (tutorial) -> Playing -> {GameClear, GameOver} -> Initialize;