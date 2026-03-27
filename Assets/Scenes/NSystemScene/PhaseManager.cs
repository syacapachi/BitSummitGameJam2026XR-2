using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System;

public class PhaseManager : NetworkBehaviour
{
    public PhaseSO[] phases;
    public NEnemySpawner spawner;

    private int currentPhaseIndex = -1;
    private float timer;

    public NetworkVariable<int> syncedPhaseIndex = new NetworkVariable<int>(-1);

    public NetworkVariable<int> countdownValue = new NetworkVariable<int>(0);
    public NetworkVariable<bool> phaseFinishing = new NetworkVariable<bool>(false);

    public NetworkVariable<bool> allEnemyDeadEvent = new NetworkVariable<bool>(false);
    public NetworkVariable<int> lastClearBonus = new NetworkVariable<int>(0);
    public NetworkVariable<float> phaseProgress = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isCountingDown = false;
    public bool isSkip = false;
    private bool isPhaseStart = false;

    public event Action OnGameClear;
    public event Action<int> OnPhaseChange;
    public event Action<int> OnPhaseClearBonus; // Å© ScoreManagerÇ…ìnÇ∑óp

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawner == null)
            spawner = GetComponentInChildren<NEnemySpawner>();
    }

    public void StartPhases()
    {
        Debug.Log("StartPhases called: " + this.name);
        if (!IsServer) return;

        currentPhaseIndex = -1;
        StartNextPhase();
    }

    void Update()
    {
        if (!IsServer) return;
        if (currentPhaseIndex >= phases.Length) return;
        if (isCountingDown) return;
        if (!isPhaseStart) return;
        if (ManagerLocator.Instance.AllGameManager.gameState.Value != GameState.Playing)
            return;

        timer -= Time.deltaTime;
        float max = phases[currentPhaseIndex].phaseTime;

        
        phaseProgress.Value = Mathf.Clamp01(timer / max);

        if (timer <= 0 || (isSkip && spawner.AllDead()))
        {
            EndPhase();
        }
    }

    void StartNextPhase()
    {
        isPhaseStart = true;
        Debug.Log("StartNextPhase called: " + this.name);
        currentPhaseIndex++;

        if (currentPhaseIndex >= phases.Length)
        {
            Debug.Log("GAME CLEAR");
            spawner.KillAllEnemies();
            OnGameClear?.Invoke();
            return;
        }

        StartCoroutine(StartPhaseWithCountdown(currentPhaseIndex));
    }

    IEnumerator StartPhaseWithCountdown(int phaseIndex)
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

        countdownValue.Value = 0;
        isCountingDown = false;

        syncedPhaseIndex.Value = phaseIndex;

        var phase = phases[phaseIndex];
        timer = phase.phaseTime;

        spawner.SpawnFromPhase(phase);

        OnPhaseChange?.Invoke(phaseIndex);
    }

    void EndPhase()
    {
        if (currentPhaseIndex == phases.Length - 1)
        {
            StartCoroutine(EndPhaseWithCountdown());
            return;
        }
        else
        {
            if (spawner.AllDead())
            {
                int bonus = phases[currentPhaseIndex].clearBonus;

                lastClearBonus.Value = bonus;
                OnPhaseClearBonus?.Invoke(bonus); // Å© Ç±Ç±èdóv

                StartCoroutine(AllDeadSequence());
                return;
            }
        }

        StartNextPhase();
    }

    IEnumerator AllDeadSequence()
    {
        isCountingDown = true;

        allEnemyDeadEvent.Value = true;

        yield return new WaitForSeconds(3.1f);

        allEnemyDeadEvent.Value = false;
        isCountingDown = false;

        StartNextPhase();
    }

    private IEnumerator EndPhaseWithCountdown()
    {
        isCountingDown = true;

        int count = 3;
        float interval = 7f / 3f; // ñÒ2.33ïb

        while (count > 0)
        {
            countdownValue.Value = count;
            Debug.Log("End Phase Countdown: " + count);

            yield return new WaitForSeconds(interval);
            count--;
        }

        countdownValue.Value = 0;

        Debug.Log("FINISH!");

        isCountingDown = false;

        StartNextPhase();
    }

    public void EnemyKilled(int score)
    {
        if (!IsServer) return;
        spawner.EnemyKilled();
    }

}