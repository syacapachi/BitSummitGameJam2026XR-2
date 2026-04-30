using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.HDROutputUtils;

public class ResultUI : MonoBehaviour
{
    private NetworkGameManager nGameManager;

    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI coopText;
    [SerializeField] EnemyDataBase enemyDatabase;
    [Header("Prefab")]
    [SerializeField] private GameObject enemyRowPrefab;
    [SerializeField] GameObject playerHeaderPrefab;
    [Header("ContentPos")]
    [SerializeField] private Transform contentParent;
    [Header("Font Setting")]
    [SerializeField] Font textFont;                 // 必要に応じて設定
    [SerializeField] int fontSizeHeader = 28;
    [SerializeField] int fontSizeStats = 22;
    [SerializeField] int fontSizeKill = 20;
    [Header("テキスト設定")]
    [SerializeField] LocalizeSimpleText gameClearText;
    [SerializeField] LocalizeSimpleText gameOverText;
    [SerializeField] LocalizeSimpleText youText;
    [SerializeField] LocalizeSimpleText otherText;
    [SerializeField] LocalizeSimpleText scoreText;
    [SerializeField] LocalizeSimpleText killsText;
    [SerializeField] LocalizeSimpleText HitsText;
    [SerializeField] LocalizeSimpleText bonusText;
    [SerializeField] LocalizeSimpleText shieldText;
    [SerializeField] LocalizeSimpleText damageText;

    [Header("SubscribeEvent")]
    [SerializeField] GameStateEvent gameStateEvent;
    [SerializeField] PlayerResultDataArrayEvent OnGameResultRpc;

    private bool isGameOver = false;
    IEnumerator Start()
    {
        InitializeUI();
        // GameManager待機
        while (ManagerLocator.Instance.AllGameManager == null)
        {
            yield return null;
        }
        nGameManager = ManagerLocator.Instance.AllGameManager;
    }

    void InitializeUI()
    {
        panel.SetActive(false);

    }
    private void OnEnable()
    {
        // イベント登録
        OnGameResultRpc.Register(OnGameFinished);
        gameStateEvent.Register(OnGameStateChanged);
    }
    private void OnDisable()
    {
        OnGameResultRpc.Unregister(OnGameFinished);
        gameStateEvent.Unregister(OnGameStateChanged);
    }

    void OnGameFinished(PlayerResultData[] resultData)
    {
        foreach (var r in resultData)
        {
            Debug.Log($"Player {r.clientId} Score:{r.score} Kills:{string.Join(",", r.killCounts)}");
        }
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        ShowResult(resultData, isJapanese);
        ShowDetail(resultData, isJapanese);
    }
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Initializing:
                InitializeUI(); break;
            case GameState.GameClear:
                isGameOver = false; break;
            case GameState.GameOver:
                isGameOver = true; break;

        }
    }
    void ShowResult(PlayerResultData[] results, bool isJapanese)
    {
        panel.SetActive(true);

        int score = nGameManager.ScoreManager.GetScore();
        int bonus = nGameManager.ScoreManager.TotalBonus;

        float cooperation = CalculateCooperation(results);

        // ⭐タイトル分岐
        if (isGameOver)
        {
            titleText.text = gameOverText.GetText(isJapanese);
        }
        else
        {
            titleText.text = gameClearText.GetText(isJapanese);
        }

        coopText.text = isJapanese
            ? $"協力度 : {cooperation:F1}%"
            : $"Cooperation : {cooperation:F1}%";

        resultText.text =
            $"{scoreText.GetText(isJapanese)} : {score}\n" +
            $"{bonusText.GetText(isJapanese)} : {bonus}";
    }

    

    void ShowDetail(PlayerResultData[] results,bool isJapanese)
    {
        panel.SetActive(true);

        titleText.text = isGameOver 
            ? gameOverText.GetText(isJapanese) 
            : gameClearText.GetText(isJapanese);

        // （必要なら）前回削除
        foreach (Transform child in contentParent)
        {
            if (child.gameObject == resultText.gameObject || child.gameObject == titleText.gameObject) continue;
            Destroy(child.gameObject);
        }

        foreach (var playerResult in results)
        {
            var headerObj = Instantiate(playerHeaderPrefab,contentParent);
            //headerObj.transform.SetParent(contentParent, false);
            // RectTransformの設定
            if(!headerObj.TryGetComponent<TextMeshProUGUI>(out var headerText))
            {
                headerText = headerObj.AddComponent<TextMeshProUGUI>();
            }
            //var rect = headerObj.GetComponent<RectTransform>();
            //rect.sizeDelta = new Vector2(400, 120); // 高さを120に固定。横は0で親に合わせる 
            headerText.fontSize = fontSizeHeader;
            headerText.alignment = TextAlignmentOptions.TopLeft;
            headerText.enableAutoSizing = false;
            headerText.text = NetworkManager.Singleton.LocalClientId == playerResult.clientId
                ? $"{youText.GetText(isJapanese)} : {playerResult.playerName}\n"
                : $"{otherText.GetText(isJapanese)} : {playerResult.playerName}\n";


            // 統計情報（2項目ずつ改行）
            string[] stats = new string[]
            {
                $"{scoreText.GetText(isJapanese)}: {playerResult.score}",
                $"{HitsText.GetText(isJapanese)}: {playerResult.hits}/{playerResult.shotsFired}",
                $"{shieldText.GetText(isJapanese)}: {playerResult.shield}",
                $"{damageText.GetText(isJapanese)}: {playerResult.damageDealt:F1}"
            };

            string statsText = "";
            for (int i = 0; i < stats.Length; i += 2)
            {
                statsText += stats[i];
                if (i + 1 < stats.Length)
                    statsText += "  " + stats[i + 1];
                statsText += "\n";
            }

            headerText.text += statsText;

            // 🔴 敵ごとに行生成
            for (int i = 0; i < playerResult.killCounts.Length; i++)
            {
                int count = playerResult.killCounts[i];
                if (count <= 0) continue;

                var enemy = enemyDatabase.GetEnemyDataFromId(i);

                var obj = Instantiate(enemyRowPrefab, contentParent);
                var row = obj.GetComponent<EnemyResultRow>();

                row.Setup(enemy.Icon, enemy.EnemyName, count);
                //レイアウトを手動で即時更新
                //ContentSizeFitter contentSizeFitter = obj.GetComponent<ContentSizeFitter>();
                //contentSizeFitter.SetLayoutHorizontal();
                //contentSizeFitter.SetLayoutVertical();

                //LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.GetComponent<RectTransform>());
            }

            var sepObj = new GameObject("Separator");
            sepObj.transform.SetParent(contentParent);
            var sepText = sepObj.AddComponent<TextMeshProUGUI>();
            sepText.text = "----------------------------";
            sepText.fontSize = fontSizeKill;
            sepText.alignment = TextAlignmentOptions.Center;
        }
    }

    float CalculateCooperation(PlayerResultData[] results)
    {
        //if (results == null || results.Length < 2) return 0f;

        float totalShots = 0;
        float totalHits = 0;
        float totalKills = 0;
        float totalShield = 0;

        foreach (var r in results)
        {
            totalShots += r.shotsFired;
            totalHits += r.hits;
            totalShield += r.shield;

            foreach (var k in r.killCounts)
            {
                totalKills += k;
            }
        }

        float accuracy = totalHits / Mathf.Max(1, totalShots);
        float killEfficiency = totalKills / Mathf.Max(1, totalHits);
        float waste = totalShield / Mathf.Max(1, totalShots);

        float cooperation =
            (accuracy * 0.5f +
             killEfficiency * 0.5f
             - waste * 0.2f) * 100f;

        return Mathf.Clamp(cooperation, 0f, 100f);
    }
}