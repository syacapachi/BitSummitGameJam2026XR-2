using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;

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