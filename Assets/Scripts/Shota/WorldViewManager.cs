/*
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class WorldViewManager : NetworkBehaviour
{
    [Header("看板のテキスト表示欄")]
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

    [Header("ローカライズテキスト")]
    public LocalizedText localizedText;

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("環境音")]
    public AudioSource ambientSource;
    public AudioClip ambientClip;

    private int currentIndex = 0;
    private int totalBoards = 4;

    private string[] japaneseTitles = {
        "双子の霊媒師",
        "霊力の法則",
        "今回の依頼",
        "操作説明"
    };

    private string[] englishTitles = {
        "Twin Mediums",
        "Law of Spiritual Power",
        "The Mission",
        "Controls"
    };

    void Start()
    {
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (ambientSource != null && ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        ShowBoard(currentIndex);
    }

    void ShowBoard(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (localizedText != null && boardText != null)
            boardText.text = localizedText.Get(index);

        if (titleText != null)
            titleText.text = isJapanese ? japaneseTitles[index] : englishTitles[index];

        // 次へボタンのテキスト
        if (buttonText != null)
        {
            if (index >= totalBoards - 1)
                buttonText.text = isJapanese ? "閉じる" : "Close";
            else
                buttonText.text = isJapanese ? "次へ" : "Next";
        }

        // 戻るボタンのテキスト
        if (backButtonText != null)
            backButtonText.text = isJapanese ? "戻る" : "Back";

        // 1枚目は戻るボタンを非表示
        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    // 次へボタン
    /*
    public void OnNextButtonClicked()
    {
        currentIndex++;
        if (currentIndex < totalBoards)
            ShowBoard(currentIndex);
        else
            SceneManager.LoadScene("TutorialScene");
    }
*/
/*    

    public void OnNextButtonClicked()
    {
        currentIndex++;

        if (currentIndex < totalBoards)
        {
            ShowBoard(currentIndex);
        }
        else
        {
            if (NetworkManager.Singleton.IsServer)
            {
                MoveScene();
            }
            else
            {
                RequestMoveSceneRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestMoveSceneRpc()
    {
        MoveScene();
    }

    void MoveScene()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(
            "TutorialScene",
            LoadSceneMode.Single
        );
    }

    // 戻るボタン
    public void OnBackButtonClicked()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowBoard(currentIndex);
        }
    }
}
*/


using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldViewManager : NetworkBehaviour
{
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
    public LocalizedText localizedText;


    public override void OnNetworkSpawn()
    {
        Debug.Log($"[WorldView] Spawned clientId={NetworkManager.LocalClientId}");

        pageIndex.OnValueChanged += OnPageChanged;

        ShowBoard(pageIndex.Value);
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
        if (pageIndex.Value >= localizedText.Length)
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
        ShowBoard(newValue);
    }

    void ShowBoard(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";


        // =========================
        // ローカライズ本文
        // =========================
        TitileAndText titleAndText = localizedText.Get(index);
        if (boardText != null)
        {
            if (string.IsNullOrEmpty(titleAndText.DescriptionText))
            {
                Debug.LogWarning($"[WorldView] LocalizedText missing index={index}");
            }

            boardText.text = titleAndText.DescriptionText;
        }

        // =========================
        // タイトル
        // =========================
        if (titleText != null)
            titleText.text = titleAndText.Title;

        // =========================
        // 次へボタン
        // =========================
        if (buttonText != null)
        {
            if (index >= localizedText.Length)
                buttonText.text = isJapanese ? "閉じる" : "Close";
            else
                buttonText.text = isJapanese ? "次へ" : "Next";
        }

        // =========================
        // 戻るボタン
        // =========================
        if (backButtonText != null)
            backButtonText.text = isJapanese ? "戻る" : "Back";

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    // =========================
    // UI UPDATE
    // =========================
    void ShowPage(int index)
    {
        bool isJP = PlayerPrefs.GetString("Language", "JP") == "JP";
        TitileAndText titleAndText = localizedText.Get(index);
        // ---- 本文 ----
        if (boardText != null)
            boardText.text = $"Page {index + 1}";

        // ---- タイトル ----
        if (titleText != null)
            titleText.text = titleAndText.Title;

        // ---- 次へボタン ----
        if (buttonText != null)
        {
            if (index >= localizedText.Length)
                buttonText.text = isJP ? "閉じる" : "Close";
            else
                buttonText.text = isJP ? "次へ" : "Next";
        }

        // ---- 戻るボタン ----
        if (backButtonText != null)
            backButtonText.text = isJP ? "戻る" : "Back";

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    // =========================
    // SCENE MOVE
    // =========================
    void MoveScene()
    {
        if (!IsServer) return;

        Debug.Log("[WorldView] Loading TutorialScene");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "TutorialScene",
            LoadSceneMode.Single
        );
    }
}
