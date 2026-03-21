using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    NGameManager gameManager;

    void Start()
    {
        TryRegister();
    }

    void TryRegister()
    {
        gameManager = ManagerLocator.Instance?.AllGameManager;

        if (gameManager == null)
        {
            Invoke(nameof(TryRegister), 0.5f);
            return;
        }

        // 初期表示
        UpdateScore(gameManager.score.Value);

        // スコア変更イベント
        gameManager.score.OnValueChanged += OnScoreChanged;
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
            gameManager.score.OnValueChanged -= OnScoreChanged;
    }
}