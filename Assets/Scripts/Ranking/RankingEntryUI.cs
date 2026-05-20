using TMPro;
using UnityEngine;
using Syacapachi.Data;

public class RankingEntryUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI cooperationText;
    [SerializeField] TextMeshProUGUI remainHPText;
    [SerializeField] TextMeshProUGUI difficultyText;

    public void Setup(int rank, ResultData data, bool isJapanese)
    {
        rankText.text = rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{rank}th"
        };

        cooperationText.text = $"{data.Cooperation:F1}%";
        remainHPText.text = $"{data.RemainHP}";

        difficultyText.text = isJapanese
            ? data.Difficulty switch
            {
                Difficulty.Easy => "イージー",
                Difficulty.Normal => "ノーマル",
                Difficulty.Hard => "ハード",
                _ => data.Difficulty.ToString()
            }
            : data.Difficulty.ToString();

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
