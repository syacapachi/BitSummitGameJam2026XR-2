using UnityEngine;
using UnityEngine.SceneManagement;

public class LanguageSelectManager : MonoBehaviour
{
    // 日本語ボタンが押されたとき
    public void OnJapaneseButtonClicked()
    {
        PlayerPrefs.SetString("Language", "JP");
        PlayerPrefs.Save();
        SceneManager.LoadScene("WorldViewScene");
    }

    // 英語ボタンが押されたとき
    public void OnEnglishButtonClicked()
    {
        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();
        SceneManager.LoadScene("WorldViewScene");
    }
}
