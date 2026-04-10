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
    [SerializeField] BGMManager bgmManager;

    SkyColer skybox;

    public ScoreManager ScoreManager => scoreManager;
    public PhaseManager PhaseManager => phaseManager;
    public GameObject ProtectArea => protectArea;
    public bool IsGamePlaying => CurrentGameState == GameState.Playing;
    public bool IsGameOver => CurrentGameState == GameState.GameOver;

    [SerializeField] NetworkVariable<GameState> gameState = new(
        GameState.Waiting,
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
    public event Action OnbulletComeRpcEvent;
    public event Action<PlayerResultData[]> OnGameResultRpc;

    public event Action OnGameStartRpcEvent;
    public event Action OnGameResetRpcEvent;
    public event Action OnGameClearRpcEvent;
    public event Action OnGameOverRpcEvent;
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

        if (IsClient)
        {
            OnGameStartRpcEvent += bgmManager.OnGameStart;
            OnGameResetRpcEvent += bgmManager.OnGameReset;
            OnGameClearRpcEvent += bgmManager.OnGameClear;
            OnGameOverRpcEvent += bgmManager.OnGameOver;
        }

        if (!IsServer) return;

        // 🔗 イベント接続
        scoreManager.OnScoreReachZero += HandleScoreZero;
        phaseManager.OnAllPhaseEnded += HandleAllPhaseEnded;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // 🔗 イベント切断
        scoreManager.OnScoreReachZero -= HandleScoreZero;
        phaseManager.OnAllPhaseEnded -= HandleAllPhaseEnded;
    }

    [OnInspectorButton("Start Game")]
    public void StartGame()
    {
        if (!IsServer) return;
        if (CurrentGameState != GameState.Waiting) return;

        Debug.Log("Game Start");
        gameState.Value = GameState.Playing;
        scoreManager.SetScoreServerOnly();
        phaseManager.StartPhases();
    }

    [OnInspectorButton("Reset")]
    public void ResetGame()
    {
        if (!IsServer) return;

        Debug.Log("GAME RESET");

        CurrentGameState = GameState.Waiting;

        phaseManager.ResetPhase();
        phaseManager.KillableHandle.KillAll();
        scoreManager.ResetScore();
    }

    void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                if (oldState == GameState.Waiting)
                    OnGameStartRpcEvent?.Invoke();
                break;
            case GameState.Waiting:
                OnGameResetRpcEvent?.Invoke();
                break;
            case GameState.GameClear:
                if(oldState == GameState.Playing)
                    OnGameClearRpcEvent?.Invoke();
                break;
            case GameState.GameOver:
                if (oldState == GameState.Playing)
                    OnGameOverRpcEvent?.Invoke();
                break;
        }
    }
    void HandleScoreZero()
    {
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
        OnbulletComeRpcEvent?.Invoke();
    }


    [Rpc(SendTo.ClientsAndHost)]
    void OnSendResultRpc(PlayerResultData[] result)
    {
        OnGameResultRpc?.Invoke(result);
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
    Waiting,     // 開始前
    Playing,     // プレイ中
    GameClear,   // クリア
    GameOver     // ゲームオーバー
}