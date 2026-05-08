using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour,ITutorialStart
{
    [Header("Reference")]
    public NetworkVariable<TutorialStep> CurrentStep =
        new(TutorialStep.Step1);

    [SerializeField] TutorialSpawner spawner;

    TutorialBase currentStepLogic;

    [SerializeField] List<EnemySO> step1Enemies;
    [SerializeField] List<EnemySO> step2Enemies;

    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] VoidEvent OnTutorialStepCleared;
    [SerializeField] IntEvent OnTutorialStepChanged;
    [Header("Move Scene")]
    [SerializeField] SceneAsset moveScene;
    private bool isTutorlalStarted;
    
    public override void OnNetworkSpawn()
    {
        isTutorlalStarted = false;
        if (IsServer)
        {
            attackBlockedEvent.Register(OnAttackBlocked);  
        }
        CurrentStep.OnValueChanged += OnStepChanged;
    }    
    public override void OnNetworkDespawn()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
        if (IsServer)
        {
            
            attackBlockedEvent.Unregister(OnAttackBlocked);
        }
    }
    public void OnTutorialStart()
    {
        if (isTutorlalStarted) return;
        isTutorlalStarted = true;
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
                currentStepLogic = new Step3_Coop(spawner, OnStepCompleted);
                break;

            case TutorialStep.Step4:
                StartMainSimulation();
                return;
            case TutorialStep.End:
                MoveScene();
                return;
            default:
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

        CurrentStep.Value++;
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
        if(!isTutorlalStarted) return;
        currentStepLogic?.OnAttackBlocked(blocked.Collector.ClientId);
    }

    public void OnEnemyKilled(EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    private void StartMainSimulation()
    {
        //応急処置
        MoveScene();
    }

    [OnInspectorButton]
    void MoveScene()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            moveScene.name,
            LoadSceneMode.Single
        );
        Debug.Log($"Move To {moveScene.name}");
    }
}
