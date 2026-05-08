using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private Canvas boardCanvas;

    [Header("ページ設定")]
    [SerializeField] private int totalBoards = 5;

    [Header("ボタンテキスト設定")]
    [SerializeField] LocalizeSimpleText nextButtonText;
    [SerializeField] LocalizeSimpleText closeText;
    [SerializeField] LocalizeSimpleText backText;

    [Header("シーン設定")]
    [SerializeField] SceneAsset tutorialScene;
    [SerializeField] SceneAsset gameScene;

    private void Start()
    {
        // connectCanvasEvent?.Invoke(); ← 削除：Start()では呼ばない
        if (!IsSpawned)
        {
            if (boardCanvas != null)
                boardCanvas.enabled = false;
        }
    }
    //OnNetworkSpanは、ネット接続時にSetActive(true)でないと呼ばれないので、Canvsのみ無効にする。
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("WorldViewManager OnNetworkSpawn called");
        if (boardCanvas != null) boardCanvas.enabled = true;
        pageIndex.OnValueChanged += OnPageChanged;
        ShowPage(pageIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        pageIndex.OnValueChanged -= OnPageChanged;
        if (boardCanvas != null) boardCanvas.enabled = false;
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
                ? closeText.GetText(isJapanese)
                : nextButtonText.GetText(isJapanese);

        if (backButtonText != null)
            backButtonText.text = backText.GetText(isJapanese);

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    void MoveScene()
    {
        if (!IsServer) return;
        if (isTutorialSkip)
        {
            Debug.Log($"[{nameof(WorldViewManager)}] Loading {gameScene.name}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameScene.name, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"[{nameof(WorldViewManager)}] Loading {tutorialScene.name}");
            NetworkManager.Singleton.SceneManager.LoadScene(tutorialScene.name, LoadSceneMode.Single);
        }
    }
}