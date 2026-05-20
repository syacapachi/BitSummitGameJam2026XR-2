using System.Collections;
using TMPro;
using UnityEngine;
using Syacapachi.Manager;
using Syacapachi.Data;

public class RankingUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] RankingManager rankingManager;

    [Header("UI Parts")]
    [SerializeField] Canvas rankingCanvas;
    [SerializeField] Transform entryParent;
    [SerializeField] GameObject entryPrefab;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI currentResultText;

    [Header("Subscribe Event")]
    [SerializeField] ResultDataEvent resultDataEvent;
    [SerializeField] GameStateEvent gameStateEvent;

    [Header("Settings")]
    [SerializeField] float showDelay = 3f;

    private bool isJapanese;

    void Start()
    {
        isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        rankingCanvas.enabled =false;
    }

    void OnEnable()
    {
        resultDataEvent.Register(OnResultReceived);
        gameStateEvent.Register(OnGameStateChanged);
    }

    void OnDisable()
    {
        resultDataEvent.Unregister(OnResultReceived);
        gameStateEvent.Unregister(OnGameStateChanged);
    }

    // ResultDataEventを受け取ったら表示開始
    // RankingManagerのSaveJsonと同じイベントなので
    // 1フレーム待てばSaveJson完了後に確実にデータが揃っている
    void OnResultReceived(ResultData data)
    {
        StartCoroutine(ShowRankingDelayed());
    }

    void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Initializing || newState == GameState.Home)
            HideRanking();
    }

    IEnumerator ShowRankingDelayed()
    {
        // RankingManagerのSaveJsonが完了するのを1フレーム待つ
        yield return null;
        yield return new WaitForSeconds(showDelay);
        ShowRanking();
    }

    void ShowRanking()
    {
        rankingCanvas.enabled = true;
        titleText.text = isJapanese ? "ランキング" : "RANKING";

        // 既存エントリーをクリア
        foreach (Transform child in entryParent)
            Destroy(child.gameObject);

        // ランキングデータを表示
        var rankings = rankingManager.Results;
        for (int i = 0; i < rankings.Count; i++)
        {
            var entry = Instantiate(entryPrefab, entryParent);
            var entryUI = entry.GetComponent<RankingEntryUI>();
            entryUI.Setup(i + 1, rankings[i], isJapanese);
        }

        // 今回のスコアをハイライト表示
        var current = rankingManager.CurrentResult;
        if (current != null)
        {
            currentResultText.text = isJapanese
                ? $"今回の協力度：{current.Cooperation:F1}%"
                : $"Your Score: {current.Cooperation:F1}%";
        }
    }

    void HideRanking()
    {
        StopAllCoroutines();
        rankingCanvas.enabled = false;
    }
}
