using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedText", menuName = "Game/LocalizedText")]
public class LocalizedText : ScriptableObject
{
    [SerializeField] TitileAndText[] japaneseTexts;

    [SerializeField] TitileAndText[] englishTexts;

    public int Length => Mathf.Min(japaneseTexts.Length, englishTexts.Length);

    public TitileAndText Get(int index)
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

        return new TitileAndText();
    }
}
