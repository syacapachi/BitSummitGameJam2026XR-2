using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Update()
    {
        if (NGameManager.Instance == null) return;

        int score = NGameManager.Instance.GetScore();
        scoreText.text = "Score : " + score;
    }
}