using Syacapachi.Attribute;
using UnityEngine;

public class UIService : MonoBehaviour
{
    private Langage langageSetting;

    public Langage LangageSetting => langageSetting;
    [Header("Subscribe")]
    [SerializeField] LangageEvent langEvent;

    private void OnEnable()
    {
        langEvent.Register(OnLangChange);
    }
    private void OnDisable()
    {
        langEvent.Unregister(OnLangChange);
    }
    private void OnLangChange(Langage langage)
    {
        langageSetting = langage;
    }

}
public enum Langage
{
    Japanese,
    English
}
