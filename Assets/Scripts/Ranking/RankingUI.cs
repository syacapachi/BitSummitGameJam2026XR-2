using System.Collections;
using TMPro;
using UnityEngine;
using Syacapachi.Manager;
using Syacapachi.Data;
using System;
using System.Collections.Generic;

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
    [SerializeField] int showRankings = 5;
    [Header("Text")]
    [SerializeField] LocalizeSimpleText rankingTitleText;
    [SerializeField] LocalizeSimpleText cooperationText;
    [SerializeField] LocalizeSimpleText startText;
    private static WaitForSeconds waitForShow;

    private bool isJapanese;

    void Start()
    {
        isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        rankingCanvas.enabled = false;
        waitForShow = new WaitForSeconds(showDelay);
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
        yield return waitForShow;
        ShowRanking();
    }

    void ShowRanking()
    {
        rankingCanvas.enabled = true;
        titleText.text = isJapanese ? "ランキング" : "RANKING";

        // 既存エントリーをクリア
        foreach (Transform child in entryParent)
            ManagerLocator.Instance.LocalObjectPool.Release(child.gameObject);

        // ランキングデータを表示
        var rankings = rankingManager.Results;
        float showCount = Math.Min(showRankings, rankings.Count);
        for (int i = 0; i < showCount; i++)
        {
            var entry = ManagerLocator.Instance.LocalObjectPool.Get(entryPrefab);
            entry.transform.SetParent(entryParent);
            var entryUI = entry.GetComponent<RankingEntryUI>();
            entryUI.Setup(i + 1, rankings[i], isJapanese);
        }

        // 今回のスコアをハイライト表示
        var current = rankingManager.CurrentResult;
        if (current != null)
        {
            currentResultText.text = isJapanese
                ? $"今回の協力度：{current.Cooperation:F1}%, 残り体力 {current.RemainHP}"
                : $"Your Cooperation: {current.Cooperation:F1}%, RemainHP {current.RemainHP}";
        }
    }

    void HideRanking()
    {
        StopAllCoroutines();
        rankingCanvas.enabled = false;
    }
}
