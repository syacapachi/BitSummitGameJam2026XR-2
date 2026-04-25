
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 変更お願い、Phaseが変わるごとに夕方->夜->深夜と暗くする by 水野
/// </summary>
public class PhaseManager : NetworkBehaviour
{
    [SerializeField] PhaseSO[] phases;
    [SerializeField] NetworkEnemySpawner spawner;
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
    [Header("Publish Event")]
    [SerializeField] VoidEvent OnAllPhaseEndedServerOnly;
    //public event Action OnAllPhaseEnded;
    [SerializeField] IntEvent OnPhaseChangeRpcEvent;
    //public event Action<int> OnPhaseChange;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawner == null)
            spawner = GetComponentInChildren<NetworkEnemySpawner>();
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
        OnPhaseChangeRpcEvent.Invoke(newValue);
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
            OnAllPhaseEndedServerOnly?.Invoke();
            return;
        }
        ManagerLocator.Instance.AllGameManager.ScoreManager.SetBonusServerOnly(phases[CurrentPhaseIndex].ClearBonus);
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
        OnPhaseChangeRpcEvent?.Invoke(phaseIndex);

        StartCoroutine(PhaseProgress());
    }
    IEnumerator PhaseProgress()
    {


        float max = phases[CurrentPhaseIndex].PhaseTime;
        while (timer > 0 && !spawner.IsAllDeadServerOnly)
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
        
        else if (spawner.IsAllDeadServerOnly)
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

    public void ResetPhase()
    {
        StopAllCoroutines();

        syncedPhaseIndex.Value = -1;
        CountdownValue.Value = 0;
        phaseProgress.Value = 1f;

        timer = 0f;
        IsCountingDown = false;
    }
}
