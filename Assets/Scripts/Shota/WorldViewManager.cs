using Syacapachi.Attribute;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WorldViewManager : NetworkBehaviour
{
    [Header("Refernce")]
    [SerializeField] GameStateManager gameStateManager;
    private NetworkVariable<int> pageIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI")]
    [SerializeField] TextMeshProUGUI boardText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] TextMeshProUGUI backButtonText;
    [SerializeField] Button backButton;
    [SerializeField] WorldViewData worldViewData;

    /*
    [Header("Canvas管理")]
    [SerializeField] private BoolEvent connectCanvasEvent;
    [SerializeField] private Canvas boardCanvas;
    */

    [Header("ページ設定")]
    [SerializeField] private int totalBoards = 5;

    [Header("ボタンテキスト設定")]
    [SerializeField] LocalizeSimpleText nextButtonText;
    [SerializeField] LocalizeSimpleText closeText;
    [SerializeField] LocalizeSimpleText backText;
    [Header("Subscribe Event")]
    [SerializeField] LocalStateEvent localStateEvent;
    [SerializeField] LanguageEvent languageEvent;
    private Language language;
    private bool IsJapanese => language == Language.Japanese;
    /// <summary>
    /// このオブジェクトは持てるので、位置を戻すために使う。
    /// </summary>
    Vector3 initialPos;

    /// <summary>
    /// 再接続時に初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        language = languageEvent.CurrentValue;
        this.transform.SetPositionAndRotation(initialPos, Quaternion.identity);
    }

    private void Awake()
    {
        initialPos = gameObject.transform.position;
    }

    private void OnEnable()
    {
        language = languageEvent.CurrentValue;
        localStateEvent.Register(OnLocalStateChanged);
        languageEvent.Register(OnLanguageChanged);
        pageIndex.OnValueChanged += OnPageChanged;
    }
    private void OnDisable()
    {
        localStateEvent.Unregister(OnLocalStateChanged);
        languageEvent.Unregister(OnLanguageChanged);
        pageIndex.OnValueChanged -= OnPageChanged;
    }
    //OnNetworkSpanは、ネット接続時にSetActive(true)でないと呼ばれないので、Canvsのみ無効にする。
    private void OnLocalStateChanged(LocalState localState)
    {
        if (localState == LocalState.WorldView)
        {
            //初期化
            this.transform.SetPositionAndRotation(initialPos, Quaternion.identity);
            ShowPage(pageIndex.Value, IsJapanese);
        }
    }
    private void OnLanguageChanged(Language newLanguage)
    {
        if (language == newLanguage) return;
        language = newLanguage;
        ShowPage(pageIndex.Value, IsJapanese);
    }

    public void OnNextButtonClicked() => RequestNextPageRpc();

    [Rpc(SendTo.Server)]
    void RequestNextPageRpc()
    {
        if (pageIndex.Value >= totalBoards - 1)
        {
            gameStateManager.OnGameInitializeServerOnly();
            pageIndex.Value = 0;
            return;
        }
        pageIndex.Value++;
    }

    public void OnBackButtonClicked() => RequestBackPageRpc();

    [Rpc(SendTo.Server)]
    void RequestBackPageRpc()
    {
        if (pageIndex.Value <= 0) return;
        pageIndex.Value--;
    }

    [OnInspectorButton]
    void OnPageChanged(int oldValue, int newValue) => ShowPage(newValue, IsJapanese);

    void ShowPage(int index, bool isJapanese)
    {
        if (boardText != null)
            boardText.text = isJapanese
                ? worldViewData.japaneseTexts[index].Replace("\\n", "\n")
                : worldViewData.englishTexts[index].Replace("\\n", "\n");

        if (titleText != null)
            titleText.text = isJapanese
                ? worldViewData.japaneseTitles[index]
                : worldViewData.englishTitles[index];

        if (buttonText != null)
            buttonText.text = index >= totalBoards - 1
                ? closeText.GetText(isJapanese)
                : nextButtonText.GetText(isJapanese);

        if (backButtonText != null)
            backButtonText.text = backText.GetText(isJapanese);

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    //void MoveScene()
    //{
    //    if (!IsServer) return;
    //    /*
    //    if (isTutorialSkip)
    //    {
    //        Debug.Log($"[{nameof(WorldViewManager)}] Loading {gameScene.name}");
    //        NetworkManager.Singleton.SceneManager.LoadScene(gameScene.name, LoadSceneMode.Single);
    //    }
    //    else
    //    {
    //        Debug.Log($"[{nameof(WorldViewManager)}] Loading {tutorialScene.name}");
    //        NetworkManager.Singleton.SceneManager.LoadScene(tutorialScene.name, LoadSceneMode.Single);
    //    }
    //    */
    //    Debug.Log($"[{nameof(WorldViewManager)}] Loading {gameSceneName}");
    //    NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    //}

}
