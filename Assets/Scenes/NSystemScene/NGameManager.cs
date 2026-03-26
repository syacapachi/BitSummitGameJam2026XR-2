using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Syacapachi.Attribute;
using System;
public class NGameManager : NetworkBehaviour
{
    public PhaseSO[] phases;
    private int currentPhaseIndex = -1;
    public float timer;
    public bool isSkip = false;
    
    public NEnemySpawner spawner;
    public NetworkVariable<int> score = new NetworkVariable<int>(10000);

    public GameObject protectArea;
    private bool isEnemycome = false;

    public NetworkVariable<int> syncedPhaseIndex = new NetworkVariable<int>(-1);
    bool gameStarted = false;
    public bool IsGameStart => gameStarted;
    public NetworkVariable<bool> isBulletCome = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> countdownValue = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private bool isCountingDown = false;
    public NetworkVariable<bool> phaseFinishing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> allEnemyDeadEvent = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> lastClearBonus = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action OnGameEnd;
    public event Action<int> OnPhaseChange;

    public override void OnNetworkSpawn()
    {
        Debug.Log("GameManager OnNetworkSpawn : " + IsServer + " / " + IsClient);
        syncedPhaseIndex.OnValueChanged += OnPhaseChanged;

        if (!IsServer) return;

        spawner = GetComponentInChildren<NEnemySpawner>();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("Game Start");
        gameStarted = true;

        ResetAndStartGame();
    }

    void Update()
    {
        if (!IsServer) return;
        if (currentPhaseIndex >= phases.Length) return;
        if (isCountingDown) return;
        if (!gameStarted) return;
        if (isGameOver.Value) return;

        timer -= Time.deltaTime;

        if (timer <= 0 || isSkip && spawner.AllDead())
        {
            EndPhase();
        }
    }

    [OnInspectorButton("Reset And Start Game")]
    public void ResetAndStartGame()
    {
        gameStarted = true;
        currentPhaseIndex = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
        currentPhaseIndex++;

        if (currentPhaseIndex >= phases.Length)
        {
            Debug.Log("GAME CLEAR");
            //OnGameEnd.Invoke();
            spawner.KillAllEnemies();
            OnGameEndClientRpc();
            SendResults();

            return;
        }

        StartCoroutine(StartPhaseWithCountdown(currentPhaseIndex));
    }

        private IEnumerator StartPhaseWithCountdown(int phaseIndex)
        {
            isCountingDown = true;
            int count = 3;
            while (count > 0)
            {
                countdownValue.Value = count;
                Debug.Log("Countdown: " + count);
                yield return new WaitForSeconds(1f);
                count--;
            }

            countdownValue.Value = 0; // 0でカウントダウン終了
            isCountingDown = false;

            // Phase開始
            syncedPhaseIndex.Value = phaseIndex;

            PhaseSO phase = phases[phaseIndex];
            timer = phase.phaseTime;

            spawner.SpawnFromPhase(phase);

            Debug.Log("Start Phase: " + phaseIndex);
        }

    void EndPhase()
    {
        // 通常進行
        if (currentPhaseIndex == phases.Length - 1)
        {
            StartCoroutine(EndPhaseWithCountdown());
        }
        else
        {
        if (spawner.AllDead())
        {
            int bonus = phases[currentPhaseIndex].clearBonus;
            AddScore(bonus);

            lastClearBonus.Value = bonus;
            StartCoroutine(AllDeadSequence());
            return; // ←ここ重要（すぐ次に行かない）
        }
            StartNextPhase();
        }
    }

    private IEnumerator AllDeadSequence()
    {
        isCountingDown = true;

        allEnemyDeadEvent.Value = true;

        yield return new WaitForSeconds(3.1f);

        allEnemyDeadEvent.Value = false;
        isCountingDown = false;

        // 次フェーズへ
        if (currentPhaseIndex == phases.Length - 1)
        {
            StartCoroutine(EndPhaseWithCountdown());
        }
        else
        {
            StartNextPhase();
        }
    }
    private IEnumerator EndPhaseWithCountdown()
    {
        isCountingDown = true;

        int count = 3;
        float interval = 7f / 3f; // 約2.33秒

        while (count > 0)
        {
            countdownValue.Value = count;
            Debug.Log("End Phase Countdown: " + count);

            yield return new WaitForSeconds(interval);
            count--;
        }

        countdownValue.Value = 0;

        Debug.Log("FINISH!");

        //OnGameEnd.Invoke();
        OnGameEndClientRpc();
        SendResults();

        isCountingDown = false;

        StartNextPhase();
    }

    public void EnemyKilled(int scoreValue)
    {
        //AddScore(scoreValue);
        spawner.EnemyKilled();
    }

    public void AddScore(int value)
    {
        if (value > 0) Debug.Log("Add Score");
        else
        {
            Debug.Log("Subtract Score");
        }

        score.Value += value;

        // 👇 下限を0に固定（おすすめ）
        if (score.Value < 0)
        {
            score.Value = 0;
        }

        Debug.Log("Score: " + score.Value);

        // 🔥 ゲームオーバー判定
        if (score.Value <= 0 && !isGameOver.Value)
        {
            StartGameOver();
        }
    }

    public void Enemycome()
    {
        isEnemycome = true;
    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        if (newValue < 0 || newValue >= phases.Length) return;

        PhaseSO phase = phases[newValue];

    }

    public int GetScore()
    {
        return score.Value;
    }

    public void BulletHitProtectArea(int damage)
    {
        AddScore(damage);
        isBulletCome.Value = true;
    }

    public void ResetBulletFlag()
    {
        isBulletCome.Value = false;
    }

    void StartGameOver()
    {
        if (!IsServer) return;

        Debug.Log("GAME OVER");
        isGameOver.Value = true;
        spawner.KillAllEnemies();
        //OnGameEnd.Invoke();
        OnGameEndClientRpc();
        SendResults();

        StopAllCoroutines(); // ← これ重要（カウントダウン等止める）

        isCountingDown = false;

        // 必要なら敵停止
        // spawner.StopAllEnemies();

        // 必要ならここでUIイベント用フラグも出せる
        
    }

    [ClientRpc]
    void OnGameEndClientRpc()
    {
        OnGameEnd?.Invoke();
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
