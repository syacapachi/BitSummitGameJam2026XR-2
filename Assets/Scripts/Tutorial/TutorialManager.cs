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

    [SerializeField] EnemySO[] step1Enemies;
    [SerializeField] EnemySO[] step2Enemies;
    [Header("Subscribe Event")]
    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] ULongEvent markerPlaceServerEvent;
    [Header("Publich Event")]
    [SerializeField] VoidEvent OnTutorialStepCleared;
    [SerializeField] IntEvent OnTutorialStepChanged;
    private bool isTutorlalStartedServerOnly;

    public override void OnNetworkSpawn()
    {
        isTutorlalStartedServerOnly = false;
        
        CurrentStep.OnValueChanged += OnStepChanged;
    }
    public override void OnNetworkDespawn()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
    }
    public void OnTutorialStartServerOnly()
    {
        if (!IsServer) return;
        if (isTutorlalStartedServerOnly) return;

        //　イベント購読
        attackBlockedEvent.Register(OnAttackBlockedServerEvent);
        markerPlaceServerEvent.Register(OnMarkerPlacedServerEvent);
        spawner.OnAllEnemyDead += OnAllEnemyDead;


        spawner.KillAll();

        currentStepLogic?.OnEnd();
        currentStepLogic = null;

        StopAllCoroutines();

        isTutorlalStartedServerOnly = true;
        isWaitingNext = false;

        CurrentStep.Value = TutorialStep.Step1;

        StartStep(TutorialStep.Step1);
    }

    void OnStepChanged(TutorialStep oldStep, TutorialStep newStep)
    {
        StartStep(newStep);
    }

    void StartStep(TutorialStep step)
    {
        currentStepLogic?.OnEnd();
        int playerCount = NetworkManager.ConnectedClientsIds.Count;

        switch (step)
        {
            case TutorialStep.Step1:
                currentStepLogic = new Step1_Target(playerCount, spawner, OnStepCompleted, step1Enemies);
                break;


            case TutorialStep.Step2:
                currentStepLogic = new Step2_Marker(
                    playerCount,
                    spawner,
                    OnStepCompleted,
                    step2Enemies);
                break;

            case TutorialStep.Step3:
                currentStepLogic = new Step3_Coop(
                    spawner,
                    OnStepCompleted);
                break;

            case TutorialStep.End:
                StartMainSimulationServerOnly();
                return;
        }
        // ここで黒魔術を紹介,型チェックをしないから早いぞ Unsafe.As<TutorialStep, int>(ref step);
        OnTutorialStepChanged?.Invoke((int)step);
        currentStepLogic?.OnStart();
    }

    bool isWaitingNext = false;

    void OnStepCompleted()
    {
        if (!IsServer) return;
        if (isWaitingNext) return;

        isWaitingNext = true;
        StartCoroutine(StepCompleteRoutine());
    }

    IEnumerator StepCompleteRoutine()
    {
        // UIに通知
        NotifyStepClearedClientRpc();

        yield return WaitForSecondsCache.Get(3.1f);

        isWaitingNext = false;
        NextStep();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void NotifyStepClearedClientRpc()
    {
        OnTutorialStepCleared.Invoke();
    }

    [OnInspectorButton("Next Step")]
    void NextStep()
    {
        if (!IsServer) return;

        switch (CurrentStep.Value)
        {
            case TutorialStep.Step1:
                CurrentStep.Value = TutorialStep.Step2;
                break;

            case TutorialStep.Step2:
                CurrentStep.Value = TutorialStep.Step3;
                break;

            case TutorialStep.Step3:
                CurrentStep.Value = TutorialStep.End;
                break;

        }
    }

    [Rpc(SendTo.Server)]
    public void NextStepRequretRpc()
    {
        if (CurrentStep.Value != TutorialStep.Step3) return;
        NextStep();
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

    public void OnEnemyKilledServerEvent(EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    private void StartMainSimulationServerOnly()
    {
        stateManager.OnTutorialEndServerOnly();
        isTutorlalStartedServerOnly = false;

        // イベント購読解消
        attackBlockedEvent.Unregister(OnAttackBlockedServerEvent);
        markerPlaceServerEvent.Unregister(OnMarkerPlacedServerEvent);
        spawner.OnAllEnemyDead -= OnAllEnemyDead;
    }

    private void OnMarkerPlacedServerEvent(ulong playerId)
    {
        if (!IsServer) return;

        currentStepLogic?.OnMarkerPlaced(playerId);
    }

    void OnAllEnemyDead()
    {
        if (!IsServer) return;

        if (!isTutorlalStartedServerOnly) return;

        switch (CurrentStep.Value)
        {
            case TutorialStep.Step2:
            case TutorialStep.Step3:
                TutorialClearByEnemyKillServerOnly();
                OnStepCompleted();
                break;
        }
    }
    void TutorialClearByEnemyKillServerOnly()
    {
        CurrentStep.Value = TutorialStep.End;
    }
}
