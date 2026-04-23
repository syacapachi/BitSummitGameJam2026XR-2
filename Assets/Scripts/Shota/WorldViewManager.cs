using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WorldViewManager : NetworkBehaviour
{
    public bool isTutorialSkip;

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

    private int totalBoards = 4;

    private string[] japaneseTitles =
    {
        "双子の霊媒師",
        "霊力の法則",
        "今回の依頼",
        "操作説明"
    };

    private string[] englishTitles =
    {
        "Twin Mediums",
        "Law of Spiritual Power",
        "The Mission",
        "Controls"
    };

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[WorldView] Spawned clientId={NetworkManager.LocalClientId}");

        pageIndex.OnValueChanged += OnPageChanged;

        ShowPage(pageIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        pageIndex.OnValueChanged -= OnPageChanged;
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
            titleText.text = isJapanese
                ? worldViewData.japaneseTitles[index]
                : worldViewData.englishTitles[index];

        if (buttonText != null)
            buttonText.text = index >= totalBoards - 1
                ? (isJapanese ? "閉じる" : "Close")
                : (isJapanese ? "次へ" : "Next");

        if (backButtonText != null)
            backButtonText.text = isJapanese ? "戻る" : "Back";

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
        else { 

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