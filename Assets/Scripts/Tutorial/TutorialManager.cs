using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour,ITutorialStart
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
    [SerializeField] VoidEvent OnTutorialStepCleared;
    [SerializeField] IntEvent OnTutorialStepChanged;
    [SerializeField] ULongEvent markerPlaceServerEvent;
    private bool isTutorlalStartedServerOnly;
    
    public override void OnNetworkSpawn()
    {
        isTutorlalStartedServerOnly = false;
        if (IsServer)
        {
            attackBlockedEvent.Register(OnAttackBlocked);
            markerPlaceServerEvent.Register(OnMarkerPlacedServer);
        }
        CurrentStep.OnValueChanged += OnStepChanged;
    }    
    public override void OnNetworkDespawn()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
        if (IsServer)
        {
            attackBlockedEvent.Unregister(OnAttackBlocked);
            markerPlaceServerEvent.Unregister(OnMarkerPlacedServer);
        }
    }
    public void OnTutorialStartServerOnly()
    {
        if (isTutorlalStartedServerOnly) return;

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
                currentStepLogic = new Step2_Block(playerCount, spawner, OnStepCompleted, step2Enemies);
                break;

            case TutorialStep.Step3:
                currentStepLogic = new Step3_Marker(
                    playerCount,
                    spawner,
                    OnStepCompleted);
                break;

            case TutorialStep.Step4:
                currentStepLogic = new Step4_Coop(
                    spawner,
                    OnStepCompleted);
                break;

            case TutorialStep.End:
                StartMainSimulation();
                return;
        }

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

        yield return new WaitForSeconds(3.1f);

        isWaitingNext = false;
        NextStep();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void NotifyStepClearedClientRpc()
    {
        OnTutorialStepCleared?.Invoke();
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
                CurrentStep.Value = TutorialStep.Step4;
                break;

            case TutorialStep.Step4:
                CurrentStep.Value = TutorialStep.End;
                break;
        }
    }

    [Rpc(SendTo.Server)]
    public void NextStepRequretRpc()
    {
        if (CurrentStep.Value != TutorialStep.Step4) return;
        NextStep();
    }

    // --- イベント転送 ---
    private void OnTargetDestroyed(ulong id)
    {
        if (!IsServer) return;
        currentStepLogic?.OnTargetDestroyed(id);
    }

    private void OnAttackBlocked(AttackBlocked blocked)
    {
        if (!IsServer) return;
        if(!isTutorlalStartedServerOnly) return;
        currentStepLogic?.OnAttackBlocked(blocked.Collector.ClientId);
    }

    public void OnEnemyKilled(EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    private void StartMainSimulation()
    {
        stateManager.OnTutorialEndServerOnly();
        isTutorlalStartedServerOnly = false;
    }

    private void OnMarkerPlacedServer(ulong playerId)
    {
        if (!IsServer) return;

        currentStepLogic?.OnMarkerPlaced(playerId);
    }
}
