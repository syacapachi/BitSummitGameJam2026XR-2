using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrystalHPUI : MonoBehaviour
{
    [SerializeField] NetworkGameManager nGameManager;

    [Header("UI")]
    [SerializeField] Canvas hpCanvas;
    [SerializeField] private Image scoreBar; // Filled Image
    [SerializeField] private TextMeshProUGUI scoreText;
    [Header("Reference")]
    [SerializeField] DifficultyDataBase rpcDataBase;
    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent HPInfoRpcEvent;
    [SerializeField] GameStateEvent GameStateEvent;

    private float maxScore;

    void UIInitialize()
    {
        maxScore = rpcDataBase.CurrentSetting.PlayerHP;
        // 念のため保険（0除算防止）
        maxScore = Mathf.Max(1f, maxScore);

        // 初期表示
        UpdateScoreUI(nGameManager.HPManager.remainHP.Value);
    }

    private void OnEnable()
    {
        // 変更監視
        nGameManager.HPManager.remainHP.OnValueChanged += OnScoreChanged;
        GameStateEvent.Register(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameStateEvent.Unregister(OnGameStateChanged);

        if (nGameManager != null)
        {
            nGameManager.HPManager.remainHP.OnValueChanged -= OnScoreChanged;
        }
    }

    void OnScoreChanged(int oldValue, int newValue)
    {
        UpdateScoreUI(newValue);
    }

    void UpdateScoreUI(int score)
    {
        // バー更新
        if (scoreBar != null)
        {
            scoreBar.fillAmount = Mathf.Clamp01((float)score / maxScore);
        }

        // テキスト更新
        if (scoreText != null)
        {
            scoreText.text = $"{score} / {maxScore}";
        }
    }

    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            UIInitialize();
            hpCanvas.enabled = true;
        }
        else if (state == GameState.GameOver || state == GameState.GameClear)
        {
            hpCanvas.enabled = false;
        }
    }
}