using TMPro;
using UnityEngine;
using Unity.Netcode;
using Syacapachi.Attribute;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject resetUI;
    [SerializeField] GameStateManager gameStateManager;

    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    [SerializeField] LanguageEvent languageEvent;

    [Header("Publich Event")]
    [SerializeField] PlayerJobEvent playerJobEvent;

    [Header("日英テキスト設定")]
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI resetButtonText;
    [SerializeField] LocalizeSimpleText gameStartButton;
    [SerializeField] LocalizeSimpleText gameResetButton;
    private Language language;
    private bool IsJapanese => language == Language.Japanese;

    private void Start()
    {
        GameStartHandle();
    }

    public override void OnNetworkSpawn()
    {
        language = languageEvent.CurrentValue;
        UpdateLanguageText(IsJapanese);
    }

    private void UpdateLanguageText(bool isJapanese)
    {
        if (startButtonText != null)
            startButtonText.text = gameStartButton.GetText(isJapanese);
        if (resetButtonText != null)
            resetButtonText.text = gameResetButton.GetText(isJapanese);
    }


    private void OnEnable()
    {
        language = languageEvent.CurrentValue;
        gameStateEvent.Register(OnGameStateChange);
        languageEvent.Register(OnLanguageChanged);
        UpdateLanguageText(IsJapanese);
    }

    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChange);
        languageEvent.Unregister(OnLanguageChanged);
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
                UpdateLanguageText(IsJapanese);
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
    private void OnLanguageChanged(Language newLanguage)
    {
        if (language == newLanguage) return;
        language = newLanguage;
        UpdateLanguageText(IsJapanese);
    }

    public void SelectStartGame()
    {
        StartGameRpc();
    }
    [OnInspectorButton]
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
        //resetUI.SetActive(false);

    }

    private void GameStartHandle()
    {
        startUI.SetActive(false);
        //resetUI.SetActive(false);
    }
    private void GameEndHandle()
    {
        startUI.SetActive(false);
        //resetUI.SetActive(true);
    }

    private void OnGameInitialize()
    {
        //startUI.SetActive(true);
        //難易度選択追加したためいったん非表示
        startUI.SetActive(false);
        //resetUI.SetActive(false);
    }

}
