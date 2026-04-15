using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using Syacapachi.Attribute;

public class NGameManager : NetworkBehaviour
{
    [SerializeField] GameObject protectArea;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PhaseManager phaseManager;

    [SerializeField] GameMode gameMode = GameMode.Protect;
    public GameMode CurrentGameMode => gameMode;

    SkyColer skybox;

    public ScoreManager ScoreManager => scoreManager;
    public PhaseManager PhaseManager => phaseManager;
    public GameObject ProtectArea => protectArea;
    public bool IsGamePlaying => CurrentGameState == GameState.Playing;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;

    

    [SerializeField] NetworkVariable<GameState> gameState = new(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public GameState CurrentGameState {
        get => gameState.Value;
        private set {
            if (!IsServer) return;
            if(gameState.Value == value) return;
            gameState.Value = value;
        }
    }
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnbulletComeRpcEvent;
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] PlayerResultDataArrayEvent RecieveResultRpcEvent;

    [Header("SubscribeEvent")]
    [SerializeField] VoidEvent OnScoreReachZeroServerEvent;
    [SerializeField] VoidEvent OnAllPhaseEndedServerEvent;


    private void OnEnable()
    {
        gameState.OnValueChanged += HandleGameStateChanged;
        
    }
    private void OnDisable()
    {
        gameState.OnValueChanged -= HandleGameStateChanged;
        
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // 🔗 イベント接続
        OnAllPhaseEndedServerEvent.Register(HandleAllPhaseEnded);
        OnScoreReachZeroServerEvent.Register(HandleScoreZeroServer);
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // 🔗 イベント切断
        OnAllPhaseEndedServerEvent.Unregister(HandleAllPhaseEnded);
        OnScoreReachZeroServerEvent.Unregister(HandleScoreZeroServer);
    }

    [OnInspectorButton("Start Game")]
    public void StartGameServerOnly()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Initializing) return;

        Debug.Log("Game Start");
        gameState.Value = GameState.Playing;
        scoreManager.SetScoreServerOnly();
        phaseManager.StartPhases();
    }

    [OnInspectorButton("Reset")]
    public void ResetGameServerOnly()
    {
        if (!IsServer) return;

        Debug.Log("GAME RESET");

        CurrentGameState = GameState.Initializing;

        phaseManager.ResetPhase();
        phaseManager.KillableHandle.KillAll();
        scoreManager.ResetScore();
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        OnGameStateChangeRpcEvent.Invoke(newState);
    }
    void HandleScoreZeroServer()
    {
        if (!IsServer) return;
        Debug.Log("GAME OVER");
        CurrentGameState = GameState.GameOver;
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();

        SendResults();
    }

    void HandleAllPhaseEnded()
    {
        Debug.Log("GAME CLEAR");
        CurrentGameState = GameState.GameClear;
        phaseManager.KillableHandle.KillAll();
        phaseManager.StopAllCoroutines();

        SendResults();
    }

    public void BulletHitProtectArea(int damage)
    {
        scoreManager.AddScoreServerOnly(damage);
        InvokeEventRpc();
    }
    [Rpc(SendTo.ClientsAndHost,InvokePermission = RpcInvokePermission.Server)]
    private void InvokeEventRpc()
    {
        OnbulletComeRpcEvent.Invoke();
    }


    [Rpc(SendTo.ClientsAndHost)]
    void OnSendResultRpc(PlayerResultData[] result)
    {
        RecieveResultRpcEvent.Invoke(result);
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

        OnSendResultRpc(list.ToArray());
    }
}
public enum GameState
{
    Initializing,// 開始前
    Playing,     // プレイ中
    GameClear,   // クリア
    GameOver     // ゲームオーバー
}

public enum GameMode
{
    Protect,   // 拠点防衛あり
    Survival   // 拠点なし（耐久）
}