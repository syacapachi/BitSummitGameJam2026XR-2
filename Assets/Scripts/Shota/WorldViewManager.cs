using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class WorldViewManager : MonoBehaviour
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
    public void OnNextButtonClicked()
    {
        currentIndex++;
        if (currentIndex < totalBoards)
            ShowBoard(currentIndex);
        else
            SceneManager.LoadScene("TutorialScene");
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
