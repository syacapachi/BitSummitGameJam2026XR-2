
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PhaseManager : NetworkBehaviour
{
    [Serializable]
    class DifficultyToPhase
    {
        public Difficulty Difficulity;
        public PhaseSO[] phases;
    }
    [SerializeField] DifficultyToPhase[] phaseSetting;
    [SerializeField] NetworkEnemySpawner spawner;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PhaseCountDownSettingSO uiSettings;
    [SerializeField] NetworkVariable<int> syncedPhaseIndex = new(-1);
    public NetworkVariable<int> CountdownValue = new(0);
    
    public NetworkVariable<bool> phaseFinished = new (false);

    public NetworkVariable<float> phaseProgress = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public IKillable KillableHandle => spawner;
    public ISpawnable SpawnableHandle => spawner;
    public PhaseSO[] Phases => currentPhaseMode.phases;
    public int CurrentPhaseIndex => syncedPhaseIndex.Value;
    private bool isInitialize = false;
    private readonly Dictionary<Difficulty, DifficultyToPhase> difficultToPhaseDic = new();
    private bool IsCountingDown = false;
    private DifficultyToPhase currentPhaseMode;
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnAllPhaseEndedServerOnly;
    [SerializeField] IntEvent OnPhaseChangeRpcEvent;
    public NetworkVariable<Difficulty> syncedDifficulty =
    new NetworkVariable<Difficulty>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private void CreateDic()
    {
        if (isInitialize) return;
        isInitialize = true;
        foreach (var setting in phaseSetting)
        {
            difficultToPhaseDic[setting.Difficulity] = setting;
        }
#if UNITY_EDITOR
        foreach (var diff in Enum.GetValues(typeof(Difficulty)))
        {
            if (!difficultToPhaseDic.ContainsKey((Difficulty)diff))
            {
                Debug.LogError($"{(Difficulty)diff} is not setting");
            }
        }
#endif
    }

    public override void OnNetworkSpawn()
    {
        CreateDic();

        syncedDifficulty.OnValueChanged += OnDifficultyChanged;
        syncedPhaseIndex.OnValueChanged += OnPhaseIndexChanged;

        // 初期値反映（超重要）
        OnDifficultyChanged(default, syncedDifficulty.Value);
        OnPhaseIndexChanged(-1, syncedPhaseIndex.Value);


        if (IsServer)
        {
            if (spawner == null)
                spawner = GetComponentInChildren<NetworkEnemySpawner>();
        }
    }

    void OnDifficultyChanged(Difficulty oldValue, Difficulty newValue)
    {
        Debug.Log($"[PhaseManager] Difficulty同期: {newValue}");

        currentPhaseMode = difficultToPhaseDic[newValue];
    }
    private void OnEnable()
    {
        //syncedPhaseIndex.OnValueChanged += OnPhaseChanedHaldle;
    }
    private void OnDisable()
    {
        //syncedPhaseIndex.OnValueChanged -= OnPhaseChanedHaldle;
    }
    private void OnPhaseChanedHaldle(int oldValue, int newValue)
    {
        OnPhaseChangeRpcEvent.Invoke(newValue);
    }

    public void StartPhases(Difficulty difficulity)
    {
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartPhases called");
#endif
        if (!IsServer) return;
        CreateDic();
        syncedDifficulty.Value = difficulity;
        currentPhaseMode = difficultToPhaseDic[difficulity];
        syncedPhaseIndex.Value = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartNextPhases called");
#endif

        syncedPhaseIndex.Value++;


        if (CurrentPhaseIndex >= Phases.Length)
        {
            spawner.KillAll();
            OnAllPhaseEndedServerOnly.Invoke();
            return;
        }
        scoreManager.SetBonusServerOnly(Phases[CurrentPhaseIndex].ClearBonus);
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
            yield return waitBase;
            count--;
        }

        CountdownValue.Value = 0;
        IsCountingDown = false;

        var phase = Phases[phaseIndex];
        SpawnableHandle.SpawnFromEvent(phase.SpawnEvents.ToList());
        //OnPhaseChangeRpcEvent.Invoke(phaseIndex);
        NotifyPhaseChangedClientRpc(phaseIndex);
        StartCoroutine(PhaseProgress(phase.PhaseTime));
    }
    IEnumerator PhaseProgress(float time)
    {
        //フェーズの残り時間を管理するためのタイマー
        float timer = time;
        //最大時間を保存しておくことで、UIに進行度を0~1の範囲で渡すことができる
        float max = time;
        while (timer > 0 && !spawner.IsAllDeadServerOnly)
        {
            //コルーチンは1フレームごとに呼ばれるため、Time.deltaTimeを引いていくことで、フェーズの残り時間を管理する
            timer -= Time.deltaTime;
            if (timer <= 0) timer = 0;
            phaseProgress.Value = Mathf.Clamp01(timer / max);
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

            scoreManager.AddBonusServerOnly(bonus); // ← ここ重要

            yield return AllDeadSequence();
        }



        StartNextPhase();
    }

    IEnumerator AllDeadSequence()
    {
        IsCountingDown = true;
        
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

        IsCountingDown = false;
    }

    public void ResetPhase()
    {
        StopAllCoroutines();

        syncedPhaseIndex.Value = -1;
        CountdownValue.Value = 0;
        phaseProgress.Value = 1f;

        IsCountingDown = false;
    }

    void OnPhaseIndexChanged(int oldValue, int newValue)
    {
        Debug.Log($"[NetworkVariable] PhaseChange: {newValue}");
        OnPhaseChangeRpcEvent.Invoke(newValue);

    }

    [ClientRpc]
    void NotifyPhaseChangedClientRpc(int index)
    {
        OnPhaseChangeRpcEvent.Invoke(index);
    }
}
