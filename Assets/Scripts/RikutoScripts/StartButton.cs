using TMPro;
using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject resetUI;
    [SerializeField] GameStateManager gameStateManager;

    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;

    [Header("Publich Event")]
    [SerializeField] PlayerJobEvent playerJobEvent;

    [Header("日英テキスト設定")]
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI resetButtonText;
    [SerializeField] LocalizeSimpleText gameStartButton;
    [SerializeField] LocalizeSimpleText gameResetButton;

    private void Start()
    {
        GameStartHandle();
    }

    public override void OnNetworkSpawn()
    {
        UpdateLanguageText();
    }

    private void UpdateLanguageText()
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (startButtonText != null)
            startButtonText.text = gameStartButton.GetText(isJapanese);
        if (resetButtonText != null)
            resetButtonText.text = gameResetButton.GetText(isJapanese);
    }

    private void OnEnable()
    {
        gameStateEvent.Register(OnGameStateChange);
    }

    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChange);
    }

    public void SelectHuman()
    {
        if (IsServer) return;
        playerJobEvent.Invoke(PlayerJob.Demon);
    }

    public void SelectGhost()
    {
        if (IsServer) return;
        playerJobEvent.Invoke(PlayerJob.Ghost);
    }

    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.Initializing:
                UpdateLanguageText();
                OnGameInitialize(); break;

            case GameState.Home:
            case GameState.Playing:
            case GameState.Tutorial:
                GameStartHandle(); break;

            case GameState.GameClear:
            case GameState.GameOver:
                GameEndHandle(); break;

            default: break;
        }
    }

    public void SelectStartGame()
    {
        StartGameRpc();
    }

    public void SelectResetGame()
    {
        ResetGameRpc();
    }

    [Rpc(SendTo.Server)]
    void StartGameRpc()
    {
        gameStateManager.OnGameStartServerOnly();
    }

    [Rpc(SendTo.Server)]
    void ResetGameRpc()
    {
        gameStateManager.OnBackToHomeServerOnly();
        resetUI.SetActive(false);
    }

    private void GameStartHandle()
    {
        startUI.SetActive(false);
        resetUI.SetActive(false);
    }
    private void GameEndHandle()
    {
        startUI.SetActive(false);
        resetUI.SetActive(true);
    }

    private void OnGameInitialize()
    {
        //startUI.SetActive(true);
        //難易度選択追加したためいったん非表示
        startUI.SetActive(false);
        resetUI.SetActive(false);
    }
}
