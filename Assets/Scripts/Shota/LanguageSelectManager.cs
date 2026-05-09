using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LanguageSelectManager : MonoBehaviour
{
    [SerializeField] TitleFlowManager titleFlowManager;
    // 日本語ボタンが押されたとき
    public void OnJapaneseButtonClicked()
    {
        PlayerPrefs.SetString("Language", "JP");
        PlayerPrefs.Save();
        titleFlowManager.SetState(TitleFlowState.NetworkConnect);
    }

    // 英語ボタンが押されたとき
    public void OnEnglishButtonClicked()
    {
        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();
        titleFlowManager.EnterNetworkConnect();
    }
}
