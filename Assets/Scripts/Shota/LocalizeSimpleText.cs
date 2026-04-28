using UnityEngine;

[CreateAssetMenu(fileName = "LocalizeSimpleText", menuName = "Game/LocalizeSimpleText")]
public class LocalizeSimpleText : ScriptableObject
{
    [SerializeField] string japanseseText;
    [SerializeField] string englishText;

    public string GetText(bool isJapanese)
    {
        if (isJapanese)
        {
            return japanseseText;
        }
        else
        {
            return englishText;
        }
    }
}
