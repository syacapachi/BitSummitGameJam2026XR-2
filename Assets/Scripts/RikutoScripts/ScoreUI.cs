using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;

    NetworkGameManager gameManager;

    IEnumerator Start()
    {
        ManagerLocator locator = ManagerLocator.Instance;
        while (locator == null
            || locator.AllGameManager == null
            || locator.AllGameManager.ScoreManager == null)
        {
            yield return null;
        }
        gameManager = locator.AllGameManager;
        // 初期表示
        UpdateScore(gameManager.ScoreManager.score.Value);

        // スコア変更イベント
        gameManager.ScoreManager.score.OnValueChanged += OnScoreChanged;
    }

    void OnScoreChanged(int oldValue, int newValue)
    {
        UpdateScore(newValue);
    }

    void UpdateScore(int value)
    {
        scoreText.text = "HP : " + value;
    }

    void OnDestroy()
    {
        if (gameManager != null)
            gameManager.ScoreManager.score.OnValueChanged -= OnScoreChanged;
    }
}