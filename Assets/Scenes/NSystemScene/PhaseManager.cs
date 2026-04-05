using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Linq;

public class PhaseManager : NetworkBehaviour
{
    [SerializeField] PhaseSO[] phases;
    [SerializeField] NEnemySpawner spawner;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PhaseCountDownSettingSO uiSettings;
       
    public IKillable KillableHandle => spawner;
    public ISpawnable SpawnableHandle => spawner;


    [SerializeField] NetworkVariable<int> syncedPhaseIndex = new(-1);
    public NetworkVariable<int> CountdownValue = new(0);
    
    public NetworkVariable<bool> phaseFinished = new (false);

    public NetworkVariable<float> phaseProgress = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] bool isSkip = false;
    public PhaseSO[] Phases => phases;
    public int CurrentPhaseIndex => syncedPhaseIndex.Value;

    private float timer;
    private bool IsCountingDown = false;

    public event Action AllEnemyDeadEventRpc;
    public event Action OnAllPhaseEnded;
    public event Action<int> OnPhaseChange;

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void AllEnemyDeathRpc()
    {
        AllEnemyDeadEventRpc?.Invoke();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawner == null)
            spawner = GetComponentInChildren<NEnemySpawner>();
    }
    private void OnEnable()
    {
        syncedPhaseIndex.OnValueChanged += OnPhaseChanedHaldle;
    }
    private void OnDisable()
    {
        syncedPhaseIndex.OnValueChanged -= OnPhaseChanedHaldle;
    }
    private void OnPhaseChanedHaldle(int oldValue, int newValue)
    {
        OnPhaseChange?.Invoke(newValue);
    }

    public void StartPhases()
    {
        Debug.Log("StartPhases called: " + this.name);
        if (!IsServer) return;

        syncedPhaseIndex.Value = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
        Debug.Log("StartNextPhase called: " + this.name);
        syncedPhaseIndex.Value++;

        if (CurrentPhaseIndex >= phases.Length)
        {
            Debug.Log("GAME CLEAR");
            spawner.KillAll();
            OnAllPhaseEnded?.Invoke();
            return;
        }

        StartCoroutine(StartPhaseWithCountdown(CurrentPhaseIndex));
    }
    IEnumerator StartPhaseWithCountdown(int phaseIndex)
    {
        yield return new WaitWhile(() => IsCountingDown); // カウントダウン中は待機
        IsCountingDown = true;
        
        int count = uiSettings.CountdownStart;
        //キャッシュを作ることでGCを減らす
        var waitBase = new WaitForSeconds(uiSettings.CountDownBaseDuration);
        while (count > 0)
        {
            CountdownValue.Value = count;
            Debug.Log("Countdown: " + count);
            yield return waitBase;
            count--;
        }

        CountdownValue.Value = 0;
        IsCountingDown = false;

        var phase = phases[phaseIndex];
        timer = phase.PhaseTime;
        SpawnableHandle.SpawnFromEvent(phase.SpawnEvents.ToList());
        OnPhaseChange?.Invoke(phaseIndex);

        StartCoroutine(PhaseProgress());
    }
    IEnumerator PhaseProgress()
    {
        
        float max = phases[CurrentPhaseIndex].PhaseTime;
        while (timer > 0 && !spawner.IsAllDead)
        {
            //コルーチンは1フレームごとに呼ばれるため、Time.deltaTimeを引いていくことで、フェーズの残り時間を管理する
            timer -= Time.deltaTime;
            phaseProgress.Value = Mathf.Clamp01(timer / max);
            yield return null;
        }
        if (phaseProgress.Value < 0f)
        {
            phaseProgress.Value = 0f;
        }
        StartCoroutine(EndPhase());
    }

    IEnumerator EndPhase()
    {
        if (CurrentPhaseIndex == phases.Length - 1)
        {
            yield return EndPhaseWithCountdown();
        }
        else if (spawner.IsAllDead)
        {
            int bonus = phases[CurrentPhaseIndex].ClearBonus;

            scoreManager.AddBonusServerOnly(bonus); // ← ここ重要

            yield return AllDeadSequence();
        }

        StartNextPhase();
    }

    IEnumerator AllDeadSequence()
    {
        IsCountingDown = true;

        AllEnemyDeathRpc();
        
        yield return new WaitForSeconds(3.1f);

        IsCountingDown = false;
    }

    private IEnumerator EndPhaseWithCountdown()
    {
        IsCountingDown = true;

        int count = uiSettings.CountdownStart;
        var waitlast = new WaitForSeconds(uiSettings.CountdownLastDuration);
        while (count > 0)
        {
            CountdownValue.Value = count;
            Debug.Log("End Phase Countdown: " + count);

            yield return waitlast;
            count--;
        }

        CountdownValue.Value = 0;

        Debug.Log("FINISH!");

        IsCountingDown = false;
    }
}