using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Update()
    {
        var nGameManager = ManagerLocator.Instance.GameManager;
        if (nGameManager) return;

        int score = nGameManager.GetScore();
        scoreText.text = "Score : " + score;
    }
}