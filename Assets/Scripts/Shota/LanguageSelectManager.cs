using UnityEngine;

public class LanguageSelectManager : MonoBehaviour
{
    [SerializeField] GameStateManager titleFlowManager;
    // 日本語ボタンが押されたとき
    public void OnJapaneseButtonClicked()
    {
        PlayerPrefs.SetString("Language", "JP");
        PlayerPrefs.Save();
        titleFlowManager.EnterNetworkConnect();
    }

    // 英語ボタンが押されたとき
    public void OnEnglishButtonClicked()
    {
        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();
        titleFlowManager.EnterNetworkConnect();
    }
}
