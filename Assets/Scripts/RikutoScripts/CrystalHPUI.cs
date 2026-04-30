using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrystalHPUI : MonoBehaviour
{
    private NetworkGameManager nGameManager;

    [Header("UI")]
    [SerializeField] private Image scoreBar; // Filled Image
    [SerializeField] private TextMeshProUGUI scoreText;
    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent HPInfoRpcEvent;
    [SerializeField] GameStateEvent GameStateEvent;

    private float maxScore;

    IEnumerator Start()
    {
        Debug.Log("[HPUI] Start begin",gameObject);
        // GameManager待機
        while (ManagerLocator.Instance.AllGameManager == null)
        {
            yield return null;
        }

        nGameManager = ManagerLocator.Instance.AllGameManager;
        Debug.Log($"[HPUI] GameManager取得 | score:{nGameManager.ScoreManager.score.Value}",gameObject);


        //  scoreの初期値が入るまで待つ（0対策）
        yield return new WaitUntil(() => nGameManager.ScoreManager != null);

        //  初期値をmaxとして保存
        maxScore = nGameManager.ScoreManager.InitialScore;
        Debug.Log($"[HPUI] maxScore:{maxScore}");

        // 念のため保険（0除算防止）
        maxScore = Mathf.Max(1f, maxScore);

        // 初期表示
        UpdateScoreUI(nGameManager.ScoreManager.score.Value);

        // 変更監視
        nGameManager.ScoreManager.score.OnValueChanged += OnScoreChanged;
    }

    private void OnEnable()
    {
        GameStateEvent.Register(OnGameStateChanged);
    }

    private void OnDestroy()
    {
        if (nGameManager != null)
        {
            nGameManager.ScoreManager.score.OnValueChanged -= OnScoreChanged;
            GameStateEvent.Unregister(OnGameStateChanged);
        }
    }

    void OnScoreChanged(int oldValue, int newValue)
    {
        Debug.Log($"[HPUI] OnScoreChanged {oldValue} → {newValue}",gameObject);
        UpdateScoreUI(newValue);
    }

    void UpdateScoreUI(int score)
    {
        Debug.Log($"[HPUI] UpdateUI score:{score} max:{maxScore}",gameObject);
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
        if (state == GameState.GameOver || state == GameState.GameClear)
        {
            gameObject.SetActive(false);
        }
    }
}