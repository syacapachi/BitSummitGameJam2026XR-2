using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedText", menuName = "Game/LocalizedText")]
public class LocalizedText : ScriptableObject
{
    [SerializeField] TitileAndText[] japaneseTexts;

    [SerializeField] TitileAndText[] englishTexts;

    public int Length => Mathf.Min(japaneseTexts.Length, englishTexts.Length);
#if UNITY_EDITOR
    private void OnEnable()
    {
        if (japaneseTexts != null && englishTexts != null && japaneseTexts.Length != englishTexts.Length)
        {
            Debug.LogWarning($"[{nameof(LocalizedText)}] {name} text length is not simmilar");
        }
    }
#endif

    public TitileAndText[] Get(bool isJapanese)
    {
        if (isJapanese)
        {
            return japaneseTexts;
        }
        else
        {
            return englishTexts;
        }
    }
}
