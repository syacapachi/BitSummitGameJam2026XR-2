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
            || locator.AllGameManager.HPManager == null)
        {
            yield return null;
        }
        gameManager = locator.AllGameManager;
        // 初期表示
        UpdateScore(gameManager.HPManager.remainHP.Value);

        // スコア変更イベント
        gameManager.HPManager.remainHP.OnValueChanged += OnScoreChanged;
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
            gameManager.HPManager.remainHP.OnValueChanged -= OnScoreChanged;
    }
}