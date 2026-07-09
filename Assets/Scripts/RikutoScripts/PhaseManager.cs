using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PhaseManager : NetworkBehaviour
{
    [SerializeField] DifficultyDataBase rpcDataBase;
    [SerializeField] NetworkEnemySpawner spawner;
    [SerializeField] HPManager scoreManager;
    [SerializeField] PhaseCountDownSettingSO uiSettings;
    [SerializeField] NetworkVariable<int> syncedPhaseIndex = new(-1);
    public NetworkVariable<int> CountdownValue = new(0);

    public NetworkVariable<float> phaseProgress = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public IKillable KillableHandle => spawner;
    public ISpawnable SpawnableHandle => spawner;
    public PhaseSO[] Phases => rpcDataBase.CurrentSetting.Phases;
    public int CurrentPhaseIndex => syncedPhaseIndex.Value;
    private bool IsCountingDown = false;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnAllPhaseEndedServerOnly;
    [SerializeField] IntEvent OnPhaseChangeRpcEvent;
    [SerializeField] BoolEvent WarningStateEvent;

    private void OnEnable()
    {
        syncedPhaseIndex.OnValueChanged += OnPhaseChanedHaldle;
    }

    private void OnDisable()
    {
        syncedPhaseIndex.OnValueChanged -= OnPhaseChanedHaldle;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"=== PhaseManager OnNetworkSpawn === IsServer:{IsServer} syncedPhaseIndex:{syncedPhaseIndex.Value}");

        // 初期値反映
        OnPhaseChanedHaldle(-1, syncedPhaseIndex.Value);

        if (IsServer)
        {
            if (spawner == null)
                spawner = GetComponentInChildren<NetworkEnemySpawner>();
        }
    }

    public override void OnNetworkDespawn()
    {
    }

    private void OnPhaseChanedHaldle(int oldValue, int newValue)
    {
#if UNITY_EDITOR
        Debug.Log($"[NetworkVariable] PhaseChange: {newValue}", gameObject);
#endif
        OnPhaseChangeRpcEvent.Invoke(newValue);
    }

    public void StartPhasesServerOnly(int gameSeed)
    {
        if (!IsServer) return;
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartPhasesServerOnly called", gameObject);
#endif
        SpawnableHandle.SetRandomSeedServerOnly(gameSeed);

        // syncedPhaseIndex.Value = -1 をコメントアウトしている理由：
        // ResetPhase() で既に -1 にリセット済みのため、ここで再度 -1 を代入すると
        // -1 → -1 で値の変化なしとみなされ OnValueChanged が発火しない。
        // そのためリセットせず直接 StartNextPhase() を呼ぶ。
        // syncedPhaseIndex.Value = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartNextPhases called", gameObject);
#endif
        int nextIndex = CurrentPhaseIndex + 1;

        if (nextIndex >= Phases.Length)
        {
            spawner.KillAll();
            OnAllPhaseEndedServerOnly.Invoke();
            return;
        }
        StartCoroutine(StartPhaseWithCountdown(nextIndex));
    }

    IEnumerator StartPhaseWithCountdown(int phaseIndex)
    {
        Debug.Log($"=== StartPhaseWithCountdown called phaseIndex:{phaseIndex} IsServer:{IsServer}");

        while (IsCountingDown)
        {
            yield return null;
        }
        IsCountingDown = true;

        if (phaseIndex == Phases.Length - 1)
        {
            yield return WaitForSecondsCache.Get(1f);
            WarningStateRpc(true);

            yield return WaitForSecondsCache.Get(5f);

            WarningStateRpc(false);
        }

        int count = uiSettings.CountdownStart;
        var waitBase = WaitForSecondsCache.Get(uiSettings.CountDownBaseDuration);
        while (count > 0)
        {
            //CountdownValue.Value = count;
            yield return waitBase;
            count--;
        }

        CountdownValue.Value = 0;
        IsCountingDown = false;

        Debug.Log($"=== syncedPhaseIndex changing to {phaseIndex} IsServer:{IsServer}");
        syncedPhaseIndex.Value = phaseIndex;

        var phaseSetting = Phases[phaseIndex];
        SpawnableHandle.SpawnFromEvent(phaseSetting.Setting);
        StartCoroutine(PhaseProgress(phaseSetting.PhaseTime));
    }

    IEnumerator PhaseProgress(float time)
    {
        float timer = time;
        float max = time;
        float invMax = 1f / max;
        while (timer > 0 && !spawner.IsAllDeadServerOnly)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) timer = 0;
            phaseProgress.Value = Mathf.Clamp01(timer * invMax);
            yield return null;
        }

        StartCoroutine(EndPhase());
    }

    IEnumerator EndPhase()
    {
        if (CurrentPhaseIndex == Phases.Length - 1)
        {
            yield return EndPhaseWithCountdown();
        }
        else if (spawner.IsAllDeadServerOnly)
        {
            int bonus = Phases[CurrentPhaseIndex].ClearBonus;
            scoreManager.AddBonusHPServerOnly(bonus);
            yield return AllDeadSequence();
        }

        StartNextPhase();
    }

    IEnumerator AllDeadSequence()
    {
        IsCountingDown = true;
        yield return WaitForSecondsCache.Get(3.1f);
        IsCountingDown = false;
    }

    private IEnumerator EndPhaseWithCountdown()
    {
        IsCountingDown = true;

        int count = uiSettings.CountdownStart;
        var waitlast = WaitForSecondsCache.Get(uiSettings.CountdownLastDuration);
        while (count > 0)
        {
            //CountdownValue.Value = count;
#if UNITY_EDITOR
            Debug.Log("End Phase Countdown: " + count, gameObject);
#endif
            yield return waitlast;
            count--;
        }

        CountdownValue.Value = 0;
        IsCountingDown = false;
    }

    public void ResetPhase()
    {
        if (!IsServer) return;
        StopAllCoroutines();

        syncedPhaseIndex.Value = -1;
        CountdownValue.Value = 0;
        phaseProgress.Value = 1f;

        IsCountingDown = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void WarningStateRpc(bool active)
    {
        WarningStateEvent.Invoke(active);
    }
}
