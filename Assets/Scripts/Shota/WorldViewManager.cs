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
    [SerializeField] private BoolEvent connectCanvasEvent;
    [SerializeField] private GameObject boardCanvas;

    [Header("ページ設定")]
    [SerializeField] private int totalBoards = 5;

    [Header("ボタンテキスト設定")]
    [SerializeField] private string japaneseNextText;
    [SerializeField] private string englishNextText;
    [SerializeField] private string japaneseCloseText;
    [SerializeField] private string englishCloseText;
    [SerializeField] private string japaneseBackText;
    [SerializeField] private string englishBackText;

    private void Start()
    {
        // connectCanvasEvent?.Invoke(); ← 削除：Start()では呼ばない
        if (boardCanvas != null) boardCanvas.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("WorldViewManager OnNetworkSpawn called");
        if (boardCanvas != null) boardCanvas.SetActive(true);
        pageIndex.OnValueChanged += OnPageChanged;
        ShowPage(pageIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        pageIndex.OnValueChanged -= OnPageChanged;
        if (boardCanvas != null) boardCanvas.SetActive(false);
    }

    public void OnNextButtonClicked() => RequestNextPageRpc();

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

    public void OnBackButtonClicked() => RequestBackPageRpc();

    [Rpc(SendTo.Server)]
    void RequestBackPageRpc()
    {
        if (pageIndex.Value <= 0) return;
        pageIndex.Value--;
    }

    void OnPageChanged(int oldValue, int newValue) => ShowPage(newValue);

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
            buttonText.text = index >= totalBoards - 1
                ? (isJapanese ? japaneseCloseText : englishCloseText)
                : (isJapanese ? japaneseNextText : englishNextText);

        if (backButtonText != null)
            backButtonText.text = isJapanese ? japaneseBackText : englishBackText;

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    void MoveScene()
    {
        if (!IsServer) return;
        if (isTutorialSkip)
        {
            Debug.Log("[WorldView] Loading VRSystemScene");
            NetworkManager.Singleton.SceneManager.LoadScene("VRSystemScene", LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("[WorldView] Loading TutorialScene");
            NetworkManager.Singleton.SceneManager.LoadScene("TutorialScene", LoadSceneMode.Single);
        }
    }
}
