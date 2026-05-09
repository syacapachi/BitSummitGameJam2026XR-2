using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LanguageSelectManager : MonoBehaviour
{
    [Header("Move Scene")]
    [SerializeField] SceneAsset moveScene;
    // 日本語ボタンが押されたとき
    public void OnJapaneseButtonClicked()
    {
        PlayerPrefs.SetString("Language", "JP");
        PlayerPrefs.Save();
        TitleFlowManager.Instance.SetState(TitleFlowState.NetworkConnect);
    }

    // 英語ボタンが押されたとき
    public void OnEnglishButtonClicked()
    {
        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();
        TitleFlowManager.Instance.EnterNetworkConnect();
    }
}
