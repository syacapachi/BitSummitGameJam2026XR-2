using TMPro;
using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject resetUI;

    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;

    [Header("Publich Event")]
    [SerializeField] PlayerJobEvent playerJobEvent;

    [Header("日英テキスト設定")]
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI resetButtonText;
    [SerializeField] LocalizeSimpleText gameStartButton;
    [SerializeField] LocalizeSimpleText gameResetButton;

    public override void OnNetworkSpawn()
    {
        startUI.SetActive(true);
        resetUI.SetActive(false);
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
                OnGameInitialize(); break;
            case GameState.Playing:
                startUI.SetActive(false); break;
            case GameState.GameClear:
                GameEndHandle(); break;
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
        ManagerLocator.Instance.AllGameManager.StartGameServerOnly();
    }

    [Rpc(SendTo.Server)]
    void ResetGameRpc()
    {
        ManagerLocator.Instance.AllGameManager.ResetGameServerOnly();
        resetUI.SetActive(false);
    }

    private void GameEndHandle()
    {
        resetUI.SetActive(true);
    }

    private void OnGameInitialize()
    {
        startUI.SetActive(true);
    }
}
