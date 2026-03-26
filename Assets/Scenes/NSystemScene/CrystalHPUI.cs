using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CrystalHPUI : NetworkBehaviour
{
    private NGameManager nGameManager;

    [Header("UI")]
    [SerializeField] private Image scoreBar; // Filled Image
    [SerializeField] private TextMeshProUGUI scoreText;

    private float maxScore;

    IEnumerator Start()
    {
        // GameManager待機
        while (ManagerLocator.Instance.AllGameManager == null)
        {
            yield return null;
        }

        nGameManager = ManagerLocator.Instance.AllGameManager;

        Debug.Log("GameManager取得成功");

        //  自分のクライアントだけ表示
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            yield break;
        }

        //  scoreの初期値が入るまで待つ（0対策）
        yield return new WaitUntil(() => nGameManager.score.Value > 0);

        //  初期値をmaxとして保存
        maxScore = nGameManager.score.Value;

        // 念のため保険（0除算防止）
        maxScore = Mathf.Max(1f, maxScore);

        // 初期表示
        UpdateScoreUI(nGameManager.score.Value);

        // 変更監視
        nGameManager.score.OnValueChanged += OnScoreChanged;
    }

    private void OnDestroy()
    {
        if (nGameManager != null)
        {
            nGameManager.score.OnValueChanged -= OnScoreChanged;
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
}