using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] NetworkGameManager gameManager;
    [SerializeField] GameStateEvent gameStateEvent;

    void Start()
    {
        // 初期表示
        UpdateScore(gameManager.HPManager.remainHP.Value);
    }
    private void OnEnable()
    {
        // スコア変更イベント
        gameManager.HPManager.remainHP.OnValueChanged += OnScoreChanged;
        gameStateEvent.Register(OnStateChanged);
    }
    private void OnDisable()
    {
        gameManager.HPManager.remainHP.OnValueChanged -= OnScoreChanged;
        gameStateEvent.Unregister(OnStateChanged);
    }
    void OnScoreChanged(int oldValue, int newValue)
    {
        UpdateScore(newValue);
    }
    void OnStateChanged(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Initializing:
            case GameState.Home:
                UpdateScore(0); break;
        }
    }
    void UpdateScore(int value)
    {
        scoreText.text = "HP : " + value;
    }
}