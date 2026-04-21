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
