using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedText", menuName = "Game/LocalizedText")]
public class LocalizedText : ScriptableObject
{
    [TextArea(3, 10)]
    public string[] japaneseTexts;

    [TextArea(3, 10)]
    public string[] englishTexts;

    public string Get(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        if (isJapanese)
        {
            if (index < japaneseTexts.Length)
                return japaneseTexts[index];
        }
        else
        {
            if (index < englishTexts.Length)
                return englishTexts[index];
        }

        return "";
    }
}
