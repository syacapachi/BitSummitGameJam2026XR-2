using TMPro;
using UnityEngine;

public class DifficultyBoardUI : MonoBehaviour
{
    [Header("Difficulty")]
    [SerializeField] Difficulty difficulty;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI titleTextGUI;
    [SerializeField] TextMeshProUGUI boardTextGUI;
    [SerializeField] TextMeshProUGUI buttonTextGUI;

    [Header("Localized")]
    [SerializeField] LocalizedText difficultyTexts;

    /// <summary>
    /// オブジェクトが、SetActive(true)されると呼ばれる。
    /// Start()は、初回SetActive(true)だけなので注意。
    /// </summary>
    void OnEnable()
    {
        Refresh();
    }
    /// <summary>
    /// 言語設定を更新する。
    /// </summary>
    public void Refresh()
    {
        bool isJapanese =
            PlayerPrefs.GetString("Language", "JP") == "JP";
        SetTitle(isJapanese);
        SetBoard(isJapanese);
        SetButton(isJapanese);
    }

    void SetTitle(bool isJapanese)
    {
        if (titleTextGUI == null) return;

        switch (difficulty)
        {
            case Difficulty.Easy:
                titleTextGUI.text =
                    isJapanese ? "イージー" : "Easy";
                break;

            case Difficulty.Normal:
                titleTextGUI.text =
                    isJapanese ? "ノーマル" : "Normal";
                break;

            case Difficulty.Hard:
                titleTextGUI.text =
                    isJapanese ? "ハード" : "Hard";
                break;
        }
    }

    void SetBoard(bool isJapanese)
    {
        if (boardTextGUI == null) return;

        TitileAndText[] texts =
            difficultyTexts.Get(isJapanese);

        int index = (int)difficulty;

        if (index < 0 || index >= texts.Length)
            return;

        boardTextGUI.text =
            texts[index].DescriptionText;

        boardTextGUI.fontSize = isJapanese ? 3.4f : 2.6f;
    }

    void SetButton(bool isJapanese)
    {
        if (buttonTextGUI == null) return;

        buttonTextGUI.text = isJapanese ? "選択" : "Select";
    }
}