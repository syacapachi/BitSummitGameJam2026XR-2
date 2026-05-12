using System.Collections;
using System.Linq;
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

    public override void OnNetworkSpawn()
    {
        // 初期値反映（超重要）
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
        Debug.Log($"[NetworkVariable] PhaseChange: {newValue}", gameObject);
        OnPhaseChangeRpcEvent.Invoke(newValue);
    }

    public void StartPhasesServerOnly()
    {
        if (!IsServer) return;
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartPhasesServerOnly called", gameObject);
#endif       
        //ここからサーバーOnlyの処理
        syncedPhaseIndex.Value = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
#if UNITY_EDITOR
        Debug.Log($"[{nameof(PhaseManager)}] {this.name} StartNextPhases called", gameObject);
#endif

        //syncedPhaseIndex.Value++;
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
        yield return new WaitWhile(() => IsCountingDown); // カウントダウン中は待機
        IsCountingDown = true;

        if (phaseIndex == Phases.Length - 1)
        {
            yield return new WaitForSeconds(1f);
            WarningStateRpc(true); // ★全員に開始通知

            yield return new WaitForSeconds(5f);

            WarningStateRpc(false); // ★全員に終了通知
        }

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
        syncedPhaseIndex.Value = phaseIndex;

        var phaseSetting = Phases[phaseIndex];
        SpawnableHandle.SpawnFromEvent(phaseSetting.SpawnEvents.ToList(), phaseSetting.UseRandomSpawn);
        //別コルーチンとして起動
        StartCoroutine(PhaseProgress(phaseSetting.PhaseTime));
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

            scoreManager.AddBonusHPServerOnly(bonus); // ← ここ重要

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
            Debug.Log("End Phase Countdown: " + count, gameObject);

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