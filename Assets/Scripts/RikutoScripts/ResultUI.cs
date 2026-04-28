using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    private NetworkGameManager nGameManager;

    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] EnemyDataBase enemyDatabase;
    [SerializeField] private GameObject enemyRowPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] Font textFont;                 // 必要に応じて設定
    [SerializeField] int fontSizeHeader = 28;
    [SerializeField] int fontSizeStats = 22;
    [SerializeField] int fontSizeKill = 20;
    [Header("テキスト設定")]
    [SerializeField] LocalizeSimpleText gameClearText;
    [SerializeField] LocalizeSimpleText gameOverText;
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
        Debug.Log("Result Received!");
        Debug.Log("Result length: " + resultData.Length);

        foreach (var r in resultData)
        {
            Debug.Log($"Player {r.clientId} Score:{r.score} Kills:{string.Join(",", r.killCounts)}");
        }
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        ShowResult(isJapanese);
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
    void ShowResult(bool isJapanese)
    {
        panel.SetActive(true);

        int score = nGameManager.ScoreManager.GetScore();
        int bonus = nGameManager.ScoreManager.TotalBonus;

        // ⭐タイトル分岐
        if (isGameOver)
        {
            titleText.text = gameOverText.GetText(isJapanese);
        }
        else
        {
            titleText.text = gameClearText.GetText(isJapanese);
        }

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
            Destroy(child.gameObject);
        }

        foreach (var playerResult in results)
        {
            var headerObj = new GameObject("PlayerHeader");
            headerObj.transform.SetParent(contentParent, false);
            // RectTransformの設定
            var rect = headerObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 120); // 高さを120に固定。横は0で親に合わせる
            var headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.fontSize = fontSizeHeader;
            headerText.alignment = TextAlignmentOptions.TopLeft;
            headerText.enableAutoSizing = false;
            headerText.text = $"Player {playerResult.clientId} : {playerResult.playerName}\n";

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
            }

            var sepObj = new GameObject("Separator");
            sepObj.transform.SetParent(contentParent, false);
            var sepText = sepObj.AddComponent<TextMeshProUGUI>();
            sepText.text = "----------------------------";
            sepText.fontSize = fontSizeKill;
            sepText.alignment = TextAlignmentOptions.Center;
        }
    }
}