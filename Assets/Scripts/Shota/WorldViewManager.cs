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

    [Header("ローカライズテキスト")]
    public LocalizedText localizedText;

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("環境音")]
    public AudioSource ambientSource;
    public AudioClip ambientClip;

    // 現在何枚目の看板を表示しているか
    private int currentIndex = 0;

    // 看板は全部で4枚（世界観3枚＋操作説明1枚）
    private int totalBoards = 4;

    // 各看板のタイトル（日本語）
    private string[] japaneseTitles = {
        "双子の霊媒師",
        "霊力の法則",
        "今回の依頼",
        "操作説明"
    };

    // 各看板のタイトル（英語）
    private string[] englishTitles = {
        "Twin Mediums",
        "Law of Spiritual Power",
        "The Mission",
        "Controls"
    };

    void Start()
    {
        // BGMを再生する
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // 環境音を再生する
        if (ambientSource != null && ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        // 最初の看板を表示する
        ShowBoard(currentIndex);
    }

    // 看板を表示するメソッド
    void ShowBoard(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        // 本文テキストを更新する
        if (localizedText != null && boardText != null)
        {
            boardText.text = localizedText.Get(index);
        }

        // タイトルテキストを更新する
        if (titleText != null)
        {
            if (isJapanese)
            {
                titleText.text = japaneseTitles[index];
            }
            else
            {
                titleText.text = englishTitles[index];
            }
        }

        // 最後の看板ならボタンのテキストを「閉じる」に変える
        if (buttonText != null)
        {
            if (index >= totalBoards - 1)
            {
                buttonText.text = isJapanese ? "閉じる" : "Close";
            }
            else
            {
                buttonText.text = isJapanese ? "次へ" : "Next";
            }
        }
    }

    // ボタンを押したときに呼ばれるメソッド
    public void OnNextButtonClicked()
    {
        currentIndex++;

        // まだ看板が残っているなら次の看板を表示
        if (currentIndex < totalBoards)
        {
            ShowBoard(currentIndex);
        }
        else
        {
            // 全部の看板を見終わったらTutorialSceneへ移動
            SceneManager.LoadScene("TutorialScene");
        }
    }
}
