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

    int score = nGameManager.GetScore();
    int bonus = nGameManager.lastClearBonus.Value;

    bool isGameOver = nGameManager.isGameOver.Value;

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
            nGameManager.OnGameEnd -= OnGameFinished;
        }
    }

    public void Show(PlayerResultData[] results)
    {
        // 自分のプレイヤー以外は無視
        if (!IsOwner) return;

        panel.SetActive(true);

        bool isGameOver = nGameManager.isGameOver.Value;

        titleText.text = isGameOver ? "GAME OVER!" : "GAME CLEAR!";

        string text = "";

        foreach (var r in results)
        {
            text += $"Player {r.clientId}\n";
            text += $"Score: {r.score}\n";
            text += $"Hits: {r.hits}/{r.shotsFired}\n\n";
        }

        resultText.text = text;
    }
}