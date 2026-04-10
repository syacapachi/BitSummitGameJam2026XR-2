using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    private NGameManager nGameManager;

    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] private EnemySO[] enemyDatabase;
    [SerializeField] private GameObject enemyRowPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] Font textFont;                 // 必要に応じて設定
    [SerializeField] int fontSizeHeader = 28;
    [SerializeField] int fontSizeStats = 22;
    [SerializeField] int fontSizeKill = 20;

    [Header("SubscribeEvent")]
    [SerializeField] PlayerResultDataArrayEvent OnGameResultRpc;
    IEnumerator Start()
    {
        // GameManager待機
        while (ManagerLocator.Instance.AllGameManager == null)
        {
            yield return null;
        }

        nGameManager = ManagerLocator.Instance.AllGameManager;

        Debug.Log("GameManager取得成功");

        InitializeUI();
    }

    void InitializeUI()
    {
        panel.SetActive(false);

    }
    private void OnEnable()
    {
        // イベント登録
        OnGameResultRpc.Register(OnGameFinished);
    }
    private void OnDisable()
    {
        OnGameResultRpc.Unregister(OnGameFinished);
    }

    void OnGameFinished(PlayerResultData[] resultData)
    {
        ShowResult();
        ShowDetail(resultData);
    }

    void ShowResult()
    {
        panel.SetActive(true);

        int score = nGameManager.ScoreManager.GetScore();
        int bonus = nGameManager.ScoreManager.totalBonus.Value;

        bool isGameOver = nGameManager.IsGameOver;

        // ⭐タイトル分岐
        if (isGameOver)
        {
            titleText.text = "GAME OVER!";
        }
        else
        {
            titleText.text = "GAME CLEAR!";
        }

        resultText.text =
            $"SCORE : {score}\n" +
            $"BONUS : {bonus}";
    }

    

    void ShowDetail(PlayerResultData[] results)
    {
        panel.SetActive(true);

        bool isGameOver = nGameManager.IsGameOver;
        titleText.text = isGameOver ? "GAME OVER!" : "GAME CLEAR!";

        // （必要なら）前回削除
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var r in results)
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
            headerText.text = $"Player {r.clientId} : {r.playerName}\n";

            // 統計情報（2項目ずつ改行）
            string[] stats = new string[]
            {
                $"Score: {r.score}",
                $"Hits: {r.hits}/{r.shotsFired}",
                $"Shield: {r.shield}",
                $"Damage: {r.damageDealt:F1}"
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
            for (int i = 0; i < r.killCounts.Length; i++)
            {
                int count = r.killCounts[i];
                if (count <= 0) continue;

                var enemy = enemyDatabase[i];

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