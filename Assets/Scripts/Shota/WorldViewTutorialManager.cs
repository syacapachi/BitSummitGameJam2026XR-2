using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class WorldTutorialManager : NetworkBehaviour
{
    [SerializeField] TutorialManager tutorialManager;

    [Header("テキスト表示欄")]
    public TextMeshProUGUI boardTextGUI;

    [Header("タイトルテキスト")]
    public TextMeshProUGUI titleTextGUI;

    [Header("次へボタン")]
    public Button nextButton;

    [Header("ボタンのテキスト")]
    public TextMeshProUGUI nextButtonTextGUI;

    [Header("戻るボタン")]
    public Button backButton;

    [Header("戻るボタンのテキスト")]
    public TextMeshProUGUI backButtonTextGUI;

    [Header("テキストデータ")]
    [SerializeField] LocalizedText tutorialTexts;
    [SerializeField] LocalizeSimpleText nextButtonText;
    [SerializeField] LocalizeSimpleText backButtonText;
    [SerializeField] LocalizeSimpleText startButtonText;

    private string[] japaneseTitles = {
        "チュートリアル1",
        "チュートリアル2",
        "チュートリアル3",
        "チュートリアル4"
    };

    private string[] englishTitles = {
        "Tutorial 1",
        "Tutorial 2",
        "Tutorial 3",
        "Tutorial 4"
    };

    private string[] japaneseTexts = {
        /*
        "右コントローラーのトリガーを引くと\n弾を発射できます。\n的に当てて練習しましょう！",
        "クリスタルに敵が触れると\nクリスタルがダメージを受けます。\n敵を倒してクリスタルを守りましょう！",
        "見えている敵は倒せません。\n霊力を使って敵を見えなくしてから\n攻撃しましょう！",
        "仲間と協力して\nクリスタルを守りましょう！\nゲームスタートです！"
        */
        "右コントローラーのトリガーを引くと\n弾を発射できます。\n全ての的を破壊しましょう！",
        "敵が出現しました。\n見えている敵は倒せません。\n目の前の敵を三回撃って確認しましょう！",
        "見えない敵は倒すことができます。\nパートナーに敵の居場所を伝えて\n攻撃しましょう！",
        "仲間と協力して\n生き残りましょう！\nゲームスタートです！"
    };

    private string[] englishTexts = {
        /*
        "Pull the right controller trigger\nto shoot bullets.\nPractice hitting the targets!",
        "If enemies touch the crystal,\nit will take damage.\nDefeat enemies to protect the crystal!",
        "Visible enemies cannot be defeated.\nUse spiritual power to make them invisible\nbefore attacking!",
        "Cooperate with your partner\nto protect the crystal!\nLet the game begin!"
        */
        "Pull the right controller trigger\nto shoot bullets.\nDestroy all the targets!",
        "Enemies have appeared.\nVisible spawnedEnemies cannot be defeated.\nShoot the enemy in front of you three times to check!",
        "Invisible spawnedEnemies can be defeated.\nCommunicate the enemy's location to your partner\nand attack!",
        "Cooperate with your partner\nand survive!\nThe game starts!"
    };

    private int totalPages = 4;
    private int currentPage;
    private int maxPage = 0;

    /*

    void Start()
    {
        ShowPage(currentIndex);
    }
    */

    public override void OnNetworkSpawn()
    {
        if (tutorialManager != null)
        {
            tutorialManager.CurrentStep.OnValueChanged += OnStepChanged;

            // 初期表示
            OnStepChanged(default, tutorialManager.CurrentStep.Value);
        }
    }

    void OnStepChanged(TutorialStep oldStep, TutorialStep newStep)
    {
        int index = (int)newStep;

        //ページ遷移[0..n),シーン遷移n。故に、[0..n]
        if (index > totalPages)
            index = totalPages;

        maxPage = Mathf.Max(maxPage, index);
        currentPage = index;
        ShowPage(index);

        // 最後のステップならボタンを「開始」に
        if (nextButtonTextGUI != null)
        {
            bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

            nextButtonTextGUI.text = newStep == TutorialStep.Step4
                ? startButtonText.GetText(isJapanese)
                : nextButtonText.GetText(isJapanese);
        }
    }
    

    void ShowPage(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        TitileAndText[] titileAndText = tutorialTexts.Get(isJapanese);
        if (boardTextGUI != null)
            boardTextGUI.text = titileAndText[index].DescriptionText;

        if (titleTextGUI != null)
            titleTextGUI.text = titileAndText[index].Title;

        if (nextButtonTextGUI != null)
            nextButtonTextGUI.text = index >= totalPages - 1
                ? startButtonText.GetText(isJapanese)
                : nextButtonText.GetText(isJapanese);

        if (backButtonTextGUI != null)
            backButtonTextGUI.text = backButtonText.GetText(isJapanese);

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);

        if(nextButton != null)
        {
            nextButton.gameObject.SetActive(index < maxPage);
        }
    }

    public void OnNextButtonClicked()
    {
        if (currentPage == totalPages - 1)
        {
            tutorialManager.NextStepRequretRpc();
            return;
        }
        if(currentPage < totalPages -1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }
    /*
    public void OnNextButtonClicked()
    {
        currentIndex++;

        if (currentIndex < totalPages)
        {
            ShowPage(currentIndex);
        }
        else
        {
            // サーバー or クライアントどちらでも押せるようにする
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
            "VRSystemScene",
            LoadSceneMode.Single
        );
    }
    */

    public void OnBackButtonClicked()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }
}
