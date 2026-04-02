using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class ResultUI : NetworkBehaviour
{
    private NGameManager nGameManager;

    public GameObject panel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI titleText;
    [SerializeField] private EnemySO[] enemyDatabase;
    [SerializeField] private GameObject enemyRowPrefab;
    [SerializeField] private Transform contentParent;
    public Font textFont;                 // 必要に応じて設定
    public int fontSizeHeader = 28;
    public int fontSizeStats = 22;
    public int fontSizeKill = 20;


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

        // イベント登録
        //nGameManager.OnGameEnd += OnGameFinished;
    }

    void OnGameFinished()
    {
        ShowResult();
    }

void ShowResult()
{
    panel.SetActive(true);

    int score = nGameManager.scoreManager.GetScore();
    int bonus = nGameManager.phaseManager.lastClearBonus.Value;

    bool isGameOver = nGameManager.scoreManager.isGameOver.Value;

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

    void OnDestroy()
    {
        if (nGameManager != null)
        {
            nGameManager.OnGameEndRpc -= OnGameFinished;
        }
    }

    public void Show(PlayerResultData[] results)
    {
        if (!IsOwner) return;

        panel.SetActive(true);

        bool isGameOver = nGameManager.scoreManager.isGameOver.Value;
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

                row.Setup(enemy.icon, enemy.Name, count);
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