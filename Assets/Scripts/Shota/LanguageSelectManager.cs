using UnityEngine;

public class LanguageSelectManager : MonoBehaviour
{
    [SerializeField] GameStateManager titleFlowManager;
    // 日本語ボタンが押されたとき
    public void OnJapaneseButtonClicked()
    {
        titleFlowManager.ChangeLanguage(Language.Japanese);
        titleFlowManager.OnLangageDefined();
    }

    // 英語ボタンが押されたとき
    public void OnEnglishButtonClicked()
    {
        titleFlowManager.ChangeLanguage(Language.English);
        titleFlowManager.OnLangageDefined();
    }
}
