using TMPro;
using UnityEngine;
using Syacapachi.Data;
using System.Runtime.CompilerServices;

public class RankingEntryUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI cooperationText;
    [SerializeField] TextMeshProUGUI remainHPText;
    private int currentRank;
    private float currentCooperation;
    private int currentRemainHP;
    private static readonly Color gold = new Color(1f, 0.84f, 0f);
    private static readonly Color silver = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color copper = new Color(0.8f, 0.5f, 0.2f);

    public void UpdateLanguage(Language langage)
    {
        SetUpInternal(currentRank, currentCooperation,currentRemainHP,langage);
    }

    public void Setup(int rank, ResultData data, bool isJapanese)
    {
        SetUpInternal(rank, data.Cooperation, data.RemainHP, isJapanese ? Language.Japanese : Language.English);
    }
    private void SetUpInternal(int rank,float cooperation,int remainHP,Language langage)
    {
        currentRank = rank;
        currentCooperation = cooperation;
        currentRemainHP = remainHP;

        rankText.text = rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{rank}th"
        };

        cooperationText.text = langage == Language.Japanese
                                ? $"協力度 {cooperation:F1}%"
                                : $"Cooperation {cooperation:F1}%";
        remainHPText.text = langage == Language.Japanese
                                ? $"残り体力 {remainHP:F1}"
                                : $"RemainHP {remainHP:F1}";

        // 順位によってテキスト色を変える
        Color rankColor = rank switch
        {
            1 => gold,        // 金
            2 => silver,  // 銀
            3 => copper,     // 銅
            _ => Color.white
        };

        rankText.color = rankColor;
        cooperationText.color = rankColor;
    }
}
