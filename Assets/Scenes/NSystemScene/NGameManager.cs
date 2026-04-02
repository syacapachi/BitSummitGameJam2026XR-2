using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using Syacapachi.Attribute;

public class NGameManager : NetworkBehaviour
{
    public GameObject protectArea;
    public ScoreManager scoreManager;
    public PhaseManager phaseManager;
    private bool gameStarted = false;
    public bool IsGameStart => gameStarted;

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Waiting,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public event Action OnbulletComeRpcEvent;
    public event Action OnGameEndRpc;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 🔗 イベント接続
        scoreManager.OnGameOver += HandleGameOver;
        phaseManager.OnGameClear += HandleGameClear;
        phaseManager.OnPhaseClearBonus += HandleBonus;
    }

    [OnInspectorButton("Start Game")]
    public void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("Game Start");
        gameStarted = true;
        gameState.Value = GameState.Playing;
        scoreManager.SetScore();
        phaseManager.StartPhases();
    }

    void HandleBonus(int bonus)
    {
        scoreManager.AddScore(bonus);
    }

    void HandleGameOver()
    {
        Debug.Log("GAME OVER");
        gameState.Value = GameState.GameOver;

        phaseManager.spawner.KillAllEnemies();

        OnGameEndClientRpc();
        SendResults();
    }

    void HandleGameClear()
    {
        Debug.Log("GAME CLEAR");
        gameState.Value = GameState.GameClear;

        OnGameEndClientRpc();
        SendResults();
    }

    public void BulletHitProtectArea(int damage)
    {
        scoreManager.AddScore(damage);
        InvokeEventRpc();
    }
    [Rpc(SendTo.ClientsAndHost,InvokePermission = RpcInvokePermission.Server)]
    private void InvokeEventRpc()
    {
        OnbulletComeRpcEvent?.Invoke();
    }


    [ClientRpc]
    void OnGameEndClientRpc()
    {
        OnGameEndRpc?.Invoke();
    }

    [ClientRpc]
    void ShowResultsClientRpc(PlayerResultData[] results)
    {
        var manager = ManagerLocator.Instance.AllPlayerManager;

        foreach (var player in manager.AllPlayers)
        {
            var ui = player.GetComponentInChildren<ResultUI>();
            if (ui != null)
            {
                ui.Show(results);
            }
        }
    }

    void SendResults()
    {
        var list = new List<PlayerResultData>();
        var manager = ManagerLocator.Instance.AllPlayerManager;

        foreach (var player in manager.AllPlayers)
        {
            if (player == null) continue;

            var stats = player.stats;
            if (stats == null) continue;

            list.Add(stats.CreateResultData());
        }

        ShowResultsClientRpc(list.ToArray());
    }
}

public enum GameState
{
    Waiting,     // 開始前
    Playing,     // プレイ中
    GameClear,   // クリア
    GameOver     // ゲームオーバー
}