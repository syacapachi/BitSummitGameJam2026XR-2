using Syacapachi.Attribute;
using Syacapachi.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class ResultUI : MonoBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);

    [SerializeField] GameObject panel;
    [SerializeField] Button enableButton;
    [SerializeField] LazyFollow follow;
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
    [SerializeField] LocalizeSimpleText CooperationText;
    [SerializeField] LocalizeSimpleText remainHPText;
    [SerializeField] LocalizeSimpleText scoreText;
    [SerializeField] LocalizeSimpleText killsText;
    [SerializeField] LocalizeSimpleText HitsText;
    [SerializeField] LocalizeSimpleText bonusText;
    [SerializeField] LocalizeSimpleText shieldText;
    [SerializeField] LocalizeSimpleText damageText;

    [Header("SubscribeEvent")]
    [SerializeField] GameStateEvent gameStateEvent;
    [SerializeField] ResultDataEvent OnGameResultRpc;

    private readonly List<GameObject> spawnedUIObjects = new();
    void Start()
    {
        UIActive(false);
        enableButton.onClick.AddListener(PanelReverce);
    }
    private void PanelReverce()
    {
        panel.SetActive(!panel.activeSelf);
    }
    private void UIActive(bool enable)
    {
        panel.SetActive(enable);
        enableButton.gameObject.SetActive(enable);
        follow.enabled = enable;
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
    void OnGameFinished(ResultData resultData)
    {
        Debug.Log($"OnGameFinished called: {Time.frameCount}");
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        ShowResult(resultData, isJapanese);
        ShowDetail(resultData, isJapanese);
        UIActive(true);
        StartCoroutine(DisableLazyFollow());

    }
    /// <summary>
    /// 個人的には、このUIがずっとついてくるのはうざいので1秒後に無力感
    /// </summary>
    /// <returns></returns>
    IEnumerator DisableLazyFollow()
    {
        yield return _waitForSeconds1;
        follow.enabled = false;
    }
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Home:
            case GameState.Initializing:
                UIActive(false); break;
        }
    }
    void ShowResult(ResultData data, bool isJapanese)
    {
        // ⭐タイトル分岐
        if (data.IsGameOver)
        {
            titleText.text = gameOverText.GetText(isJapanese);
        }
        else
        {
            titleText.text = gameClearText.GetText(isJapanese);
        }

        coopText.text =
             $"{CooperationText.GetText(isJapanese)} : {data.Cooperation:F1}%";

        resultText.text =
            $"{remainHPText.GetText(isJapanese)} : {data.RemainHP}\n" +
            $"{bonusText.GetText(isJapanese)} : {data.TotalBonusHP}";
    }
    void ShowDetail(ResultData results, bool isJapanese)
    {
        foreach (var r in results.detail)
        {
            Debug.Log($"Player {r.clientId} Score:{r.score} Kills:{string.Join(",", r.killCounts)}");
        }
        // （必要なら）前回削除
        ClearSpawnedUI();

        titleText.text = results.IsGameOver
            ? gameOverText.GetText(isJapanese)
            : gameClearText.GetText(isJapanese);


        foreach (var playerResult in results.detail)
        {
            var headerObj = Instantiate(playerHeaderPrefab, contentParent);
            spawnedUIObjects.Add(headerObj);
            if (!headerObj.TryGetComponent<TextMeshProUGUI>(out var headerText))
            {
                headerText = headerObj.AddComponent<TextMeshProUGUI>();
            }
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
                spawnedUIObjects.Add(obj);
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
            spawnedUIObjects.Add(sepObj);
            var sepText = sepObj.AddComponent<TextMeshProUGUI>();
            sepText.text = "----------------------------";
            sepText.fontSize = fontSizeKill;
            sepText.alignment = TextAlignmentOptions.Center;
        }
    }
    void ClearSpawnedUI()
    {
        foreach (var obj in spawnedUIObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedUIObjects.Clear();
    }
}