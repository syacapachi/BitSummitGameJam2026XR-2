using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class NGameManager : NetworkBehaviour
{
    public PhaseSO[] phases;
    private int currentPhaseIndex = -1;
    public float timer;

    public NEnemySpawner spawner;
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    public GameObject protectArea;
    private bool isEnemycome = false;

    public NetworkVariable<int> syncedPhaseIndex = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> IsGameFinished = new NetworkVariable<bool>(false);
    bool gameStarted = false;
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

        timer -= Time.deltaTime;

        if (timer <= 0 )
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
            if (IsServer)
            {
                Debug.Log("Game Finished");
                IsGameFinished.Value = true;
            }

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
        if (spawner.AllDead())
        {
            int bonus = phases[currentPhaseIndex].clearBonus;
            AddScore(bonus);

            lastClearBonus.Value = bonus;
            StartCoroutine(AllDeadSequence());
            return; // ←ここ重要（すぐ次に行かない）
        }

        // 通常進行
        if (currentPhaseIndex == phases.Length - 1)
        {
            StartCoroutine(EndPhaseWithCountdown());
        }
        else
        {
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

        IsGameFinished.Value = true; // ここでFINISH表示

        isCountingDown = false;
        StartNextPhase();
    }

    public void EnemyKilled(int scoreValue)
    {
        AddScore(scoreValue);
        spawner.EnemyKilled();
    }

    public void AddScore(int value)
    {
        if (value > 0) Debug.Log("Add Score");
        else
        {
            if(score.Value <= 0) return;
            Debug.Log("Subtract Score");
        }
        score.Value += value;
        Debug.Log("Score: " + score);
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
}
