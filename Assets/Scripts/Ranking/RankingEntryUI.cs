using TMPro;
using UnityEngine;
using Syacapachi.Data;

public class RankingEntryUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI cooperationText;
    [SerializeField] TextMeshProUGUI remainHPText;

    public void Setup(int rank, ResultData data, bool isJapanese)
    {
        rankText.text = rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{rank}th"
        };

        cooperationText.text = isJapanese
                                ? $"協力度 {data.Cooperation:F1}%"
                                : $"Cooperation {data.Cooperation:F1}%";
        remainHPText.text = isJapanese
                                ? $"残り体力 {data.RemainHP:F1}"
                                : $"RemainHP {data.RemainHP:F1}";

        // 順位によってテキスト色を変える
        Color rankColor = rank switch
        {
            1 => new Color(1f, 0.84f, 0f),        // 金
            2 => new Color(0.75f, 0.75f, 0.75f),  // 銀
            3 => new Color(0.8f, 0.5f, 0.2f),     // 銅
            _ => Color.white
        };

        rankText.color = rankColor;
        cooperationText.color = rankColor;
    }
}
