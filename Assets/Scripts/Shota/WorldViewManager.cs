using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WorldViewManager : NetworkBehaviour
{
    [SerializeField] private bool isTutorialSkip;

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

    [Header("Canvas管理")]
    // (connectCanvas・boardCanvasの直接SetActiveをやめてUIViewSettingのuiEventで管理)
    // [SerializeField] private GameObject connectCanvas;
    // [SerializeField] private GameObject boardCanvas;
    [SerializeField] private VoidEvent connectCanvasEvent;
    [SerializeField] private Canvas boardCanvas;

    [Header("ページ設定")]
    // private int totalBoards = 4; // 旧: ハードコード
    [SerializeField] private int totalBoards = 5;

    [Header("ボタンテキスト設定")]
    // (ハードコードしていた文字列をInspectorで設定可能に)
    [SerializeField] private string japaneseNextText;
    [SerializeField] private string englishNextText;
    [SerializeField] private string japaneseCloseText;
    [SerializeField] private string englishCloseText;
    [SerializeField] private string japaneseBackText;
    [SerializeField] private string englishBackText;

    // private string[] japaneseTitles =  // 旧: WorldViewDataに一本化したため削除
    // {
    //     "双子の霊媒師",
    //     "霊力の法則",
    //     "今回の依頼",
    //     "操作説明"
    // };

    // private string[] englishTitles =   // 旧: WorldViewDataに一本化したため削除
    // {
    //     "Twin Mediums",
    //     "Law of Spiritual Power",
    //     "The Mission",
    //     "Controls"
    // };

    // =========================
    // 起動時: 看板を非表示にしてConnectCanvasを表示
    // =========================
    private void Start()
    {
        // (UIViewSettingのuiEventを発火してConnectCanvasを表示)
        if (connectCanvasEvent != null) connectCanvasEvent.Invoke();
        // (看板は接続完了まで非表示)
        if (boardCanvas != null) boardCanvas.enabled = false;
    }

    // =========================
    // ネットワーク接続完了時
    // =========================
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("WorldViewManager OnNetworkSpawn called");

        // ConnectCanvasを非表示にする（VoidEventで切り替え）
        connectCanvasEvent?.Invoke();

        // 看板を表示
        if (boardCanvas != null)
            boardCanvas.enabled = true;

        // ページ変更の監視開始
        // pageIndex.OnValueChanged += OnPageIndexChanged; // 旧: メソッド名が誤っていたため修正
        pageIndex.OnValueChanged += OnPageChanged;

        // 初期ページ表示
        ShowPage(pageIndex.Value);
    }

    // =========================
    // ネットワーク切断時
    // =========================
    public override void OnNetworkDespawn()
    {
        // pageIndex.OnValueChanged -= OnPageChanged; // 旧: base呼び出しが抜けていたため修正
        base.OnNetworkDespawn();
        pageIndex.OnValueChanged -= OnPageChanged;
        if (boardCanvas != null) boardCanvas.enabled = false;
    }

    // =========================
    // NEXT
    // =========================
    public void OnNextButtonClicked()
    {
        RequestNextPageRpc();
    }

    [Rpc(SendTo.Server)]
    void RequestNextPageRpc()
    {
        if (pageIndex.Value >= totalBoards - 1)
        {
            MoveScene();
            return;
        }
        pageIndex.Value++;
    }

    // =========================
    // BACK
    // =========================
    public void OnBackButtonClicked()
    {
        RequestBackPageRpc();
    }

    [Rpc(SendTo.Server)]
    void RequestBackPageRpc()
    {
        if (pageIndex.Value <= 0) return;
        pageIndex.Value--;
    }

    // =========================
    // SYNC UPDATE
    // =========================
    void OnPageChanged(int oldValue, int newValue)
    {
        ShowPage(newValue);
    }

    // =========================
    // UI UPDATE
    // =========================
    void ShowPage(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (boardText != null)
            boardText.text = isJapanese
                ? worldViewData.japaneseTexts[index].Replace("\\n", "\n")
                : worldViewData.englishTexts[index].Replace("\\n", "\n");

        if (titleText != null)
            // ? worldViewData.japaneseTitles[index] はWorldViewDataに一本化したため削除
            // : worldViewData.englishTitles[index] はWorldViewDataに一本化したため削除
            titleText.text = isJapanese
                ? worldViewData.japaneseTitles[index]
                : worldViewData.englishTitles[index];

        if (buttonText != null)
            // ? (isJapanese ? "閉じる" : "Close") をInspector設定に変更
            // : (isJapanese ? "次へ" : "Next") をInspector設定に変更
            buttonText.text = index >= totalBoards - 1
                ? (isJapanese ? japaneseCloseText : englishCloseText)
                : (isJapanese ? japaneseNextText : englishNextText);

        if (backButtonText != null)
            // backButtonText.text = isJapanese ? "戻る" : "Back"; // 旧: ハードコード
            backButtonText.text = isJapanese ? japaneseBackText : englishBackText;

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);

        
    }

    // =========================
    // SCENE MOVE
    // =========================
    void MoveScene()
    {
        if (!IsServer) return;
        if (isTutorialSkip)
        {
            Debug.Log("[WorldView] Loading VRSystemScene");
            NetworkManager.Singleton.SceneManager.LoadScene(
                "VRSystemScene",
                LoadSceneMode.Single
            );
        }
        else
        {
            Debug.Log("[WorldView] Loading TutorialScene");
            NetworkManager.Singleton.SceneManager.LoadScene(
                "TutorialScene",
                LoadSceneMode.Single
            );
        }
    }
}

/*
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class WorldViewManager : MonoBehaviour
{
    [Header("テキスト表示欄")]
    public TextMeshProUGUI boardText;

    [Header("タイトルテキスト")]
    public TextMeshProUGUI titleText;

    [Header("次へボタン")]
    public Button nextButton;

    [Header("ボタンのテキスト")]
    public TextMeshProUGUI buttonText;

    [Header("戻るボタン")]
    public Button backButton;

    [Header("戻るボタンのテキスト")]
    public TextMeshProUGUI backButtonText;

    [Header("世界観データ")]
    public WorldViewData worldViewData;

    private int currentIndex = 0;
    private int totalPages = 4;

    void Start()
    {
        ShowPage(currentIndex);
    }

    void ShowPage(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (boardText != null)
            boardText.text = isJapanese
                ? worldViewData.japaneseTexts[index].Replace("\\n", "\n")
                : worldViewData.englishTexts[index].Replace("\\n", "\n");

        if (titleText != null)
            titleText.text = isJapanese
                ? worldViewData.japaneseTitles[index]
                : worldViewData.englishTitles[index];

        if (buttonText != null)
            buttonText.text = index >= totalPages - 1
                ? (isJapanese ? "閉じる" : "Close")
                : (isJapanese ? "次へ" : "Next");

        if (backButtonText != null)
            backButtonText.text = isJapanese ? "戻る" : "Back";

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    public void OnNextButtonClicked()
    {
        currentIndex++;
        if (currentIndex < totalPages)
            ShowPage(currentIndex);
        else
            SceneManager.LoadScene("TutorialScene");
    }

    public void OnBackButtonClicked()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowPage(currentIndex);
        }
    }
}
*/
