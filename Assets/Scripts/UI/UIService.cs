using Syacapachi.Attribute;
using UnityEngine;

public class UIService : MonoBehaviour
{
    private Language langageSetting;

    public Language LangageSetting => langageSetting;
    [Header("Subscribe")]
    [SerializeField] LanguageEvent langEvent;

    private void OnEnable()
    {
        langEvent.Register(OnLangChange);
    }
    private void OnDisable()
    {
        langEvent.Unregister(OnLangChange);
    }
    private void OnLangChange(Language langage)
    {
        langageSetting = langage;
    }

}
public enum Language
{
    Japanese,
    English
}
