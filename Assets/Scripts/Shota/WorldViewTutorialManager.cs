using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class WorldTutorialManager : NetworkBehaviour
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

    private int currentIndex = 0;

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
        "右コントローラーのトリガーを引くと\n弾を発射できます。\n的に当てて練習しましょう！",
        "クリスタルに敵が触れると\nクリスタルがダメージを受けます。\n敵を倒してクリスタルを守りましょう！",
        "見えている敵は倒せません。\n霊力を使って敵を見えなくしてから\n攻撃しましょう！",
        "仲間と協力して\nクリスタルを守りましょう！\nゲームスタートです！"
    };

    private string[] englishTexts = {
        "Pull the right controller trigger\nto shoot bullets.\nPractice hitting the targets!",
        "If enemies touch the crystal,\nit will take damage.\nDefeat enemies to protect the crystal!",
        "Visible enemies cannot be defeated.\nUse spiritual power to make them invisible\nbefore attacking!",
        "Cooperate with your partner\nto protect the crystal!\nLet the game begin!"
    };

    private int totalPages = 4;

    void Start()
    {
        ShowPage(currentIndex);
    }

    void ShowPage(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (boardText != null)
            boardText.text = isJapanese ? japaneseTexts[index] : englishTexts[index];

        if (titleText != null)
            titleText.text = isJapanese ? japaneseTitles[index] : englishTitles[index];

        if (buttonText != null)
            buttonText.text = index >= totalPages - 1
                ? (isJapanese ? "ゲームスタート" : "Game Start")
                : (isJapanese ? "次へ" : "Next");

        if (backButtonText != null)
            backButtonText.text = isJapanese ? "戻る" : "Back";

        if (backButton != null)
            backButton.gameObject.SetActive(index > 0);
    }

    /*
    public void OnNextButtonClicked()
    {
        currentIndex++;
        if (currentIndex < totalPages)
            ShowPage(currentIndex);
        else
            SceneManager.LoadScene("VRSystemScene");
    }
    */

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

    public void OnBackButtonClicked()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowPage(currentIndex);
        }
    }
}
