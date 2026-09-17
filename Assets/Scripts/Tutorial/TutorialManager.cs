using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour, ITutorialStart
{
    [Header("Reference")]
    [SerializeField] GameStateManager stateManager;
    public NetworkVariable<TutorialStep> CurrentStep =
        new(TutorialStep.Step1);

    [SerializeField] TutorialSpawner spawner;

    TutorialBase currentStepLogic;

    [SerializeField] EnemySO step1Enemy;
    [SerializeField] int step1EnemySpawnCount = 7;
    [SerializeField] EnemySO[] step2Enemies;
    [Header("Subscribe Event")]
    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] ULongEvent markerPlaceServerEvent;
    [SerializeField] GameStateEvent gameStateEvent;
    [Header("Publich Event")]
    [SerializeField] VoidEvent OnTutorialStepCleared;
    [SerializeField] IntEvent OnTutorialStepChanged;
    private bool isTutorlalStartedServerOnly;

    public override void OnNetworkSpawn()
    {
        isTutorlalStartedServerOnly = false;

        if (!IsServer) return;
        CurrentStep.OnValueChanged += OnStepChangedServerEvent;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        CurrentStep.OnValueChanged -= OnStepChangedServerEvent;
    }
    public void OnTutorialStartServerOnly()
    {
        if (!IsServer) return;
        if (isTutorlalStartedServerOnly) return;

        //　イベント購読
        attackBlockedEvent.Register(OnAttackBlockedServerEvent);
        markerPlaceServerEvent.Register(OnMarkerPlacedServerEvent);
        spawner.OnAllEnemyDead += OnAllEnemyDeadServerEvent;
        gameStateEvent.Register(OnGameStateChangedServerOnly);


        spawner.KillAll();

        currentStepLogic?.OnEnd();
        currentStepLogic = null;

        StopAllCoroutines();

        isTutorlalStartedServerOnly = true;

        CurrentStep.Value = TutorialStep.Step1;

        StartStepServerOnly(TutorialStep.Step1);
    }
    void OnGameStateChangedServerOnly(GameState newState)
    {
        if(newState != GameState.Tutorial)
        {
            spawner.KillAll();
            TutorialEndServerOnly();
        }
    }
    void OnStepChangedServerEvent(TutorialStep oldStep, TutorialStep newStep)
    {
        StartStepServerOnly(newStep);
    }
    [OnInspectorButton]
    void StartStepServerOnly(TutorialStep step)
    {
        currentStepLogic?.OnEnd();
        currentStepLogic = null;
        int playerCount = NetworkManager.ConnectedClientsIds.Count;

        switch (step)
        {
            case TutorialStep.Step1:
                currentStepLogic = new Step1_Target(playerCount, spawner, OnStepCompletedServerOnly, step1Enemy);
                break;


            case TutorialStep.Step2:
                currentStepLogic = new Step2_Marker(
                    playerCount,
                    spawner,
                    OnStepCompletedServerOnly,
                    step2Enemies);
                break;

            case TutorialStep.Step3:
                currentStepLogic = new Step3_Coop(
                    spawner,
                    OnStepCompletedServerOnly);
                break;

            case TutorialStep.End:
                TutorialEndServerOnly();
                return;
        }
        // ここで黒魔術を紹介,型チェックをしないから早いぞ Unsafe.As<TutorialStep, int>(ref step);
        OnTutorialStepChanged?.Invoke((int)step);
        currentStepLogic?.OnStart();
    }

    void OnStepCompletedServerOnly()
    {
        if (!IsServer) return;
        TutorialStep next = NextStep(CurrentStep.Value);
        StartCoroutine(StepCompleteRoutine(next));
    }

    IEnumerator StepCompleteRoutine(TutorialStep nextStep)
    {
        // UIに通知
        NotifyStepClearedClientRpc();

        yield return WaitForSecondsCache.Get(3.1f);

        CurrentStep.Value = nextStep;
    } 

    [Rpc(SendTo.ClientsAndHost)]
    void NotifyStepClearedClientRpc()
    {
        OnTutorialStepCleared.Invoke();
    }

    static TutorialStep NextStep(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.Step1 => TutorialStep.Step2,

            TutorialStep.Step2 => TutorialStep.Step3,

            TutorialStep.Step3 => TutorialStep.End,

            TutorialStep.Step4 => TutorialStep.End,
            
            TutorialStep.End => TutorialStep.End,

            _ => TutorialStep.End,
        };
    }

    [Rpc(SendTo.Server)]
    public void NextStepRequretRpc()
    {
        if (CurrentStep.Value != TutorialStep.Step3) return;
        TutorialStep step = NextStep(CurrentStep.Value);
        StartCoroutine(StepCompleteRoutine(step));
    }

    // --- イベント転送 ---
    private void OnTargetDestroyed(ulong id)
    {
        if (!IsServer) return;
        currentStepLogic?.OnTargetDestroyed(id);
    }

    private void OnAttackBlockedServerEvent(in AttackBlocked blocked)
    {
        if (!IsServer) return;
        if (!isTutorlalStartedServerOnly) return;
        currentStepLogic?.OnAttackBlocked(blocked.Collector.ClientId);
    }

    public void OnEnemyKilledServerEvent(in EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    private void TutorialEndServerOnly()
    {
        stateManager.OnTutorialEndServerOnly();
        isTutorlalStartedServerOnly = false;

        // イベント購読解消
        attackBlockedEvent.Unregister(OnAttackBlockedServerEvent);
        markerPlaceServerEvent.Unregister(OnMarkerPlacedServerEvent);
        spawner.OnAllEnemyDead -= OnAllEnemyDeadServerEvent;
        gameStateEvent.Unregister(OnGameStateChangedServerOnly);
    }

    private void OnMarkerPlacedServerEvent(ulong playerId)
    {
        if (!IsServer) return;

        currentStepLogic?.OnMarkerPlaced(playerId);
    }

    void OnAllEnemyDeadServerEvent()
    {
        if (!IsServer) return;

        if (!isTutorlalStartedServerOnly) return;

        switch (CurrentStep.Value)
        {
            case TutorialStep.Step2:
            case TutorialStep.Step3:
                TutorialClearByEnemyKillServerOnly();
                break;
        }
    }
    void TutorialClearByEnemyKillServerOnly()
    {
        StartCoroutine(StepCompleteRoutine(TutorialStep.End));
    }
}
