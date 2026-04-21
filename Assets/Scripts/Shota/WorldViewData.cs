using UnityEngine;

[CreateAssetMenu(fileName = "WorldViewData",
menuName = "Game/WorldViewData")]
public class WorldViewData : ScriptableObject
{
    [Header("日本語タイトル")]
    public string[] japaneseTitles;

    [Header("英語タイトル")]
    public string[] englishTitles;

    [Header("日本語テキスト")]
    public string[] japaneseTexts;

    [Header("英語テキスト")]
    public string[] englishTexts;
}
