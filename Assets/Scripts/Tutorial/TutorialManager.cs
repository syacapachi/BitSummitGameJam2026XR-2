using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.XR;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour
{
    public NetworkVariable<TutorialStep> CurrentStep =
        new(TutorialStep.Step1);

    [SerializeField] TutorialSpawner spawner;

    TutorialBase currentStepLogic;

    [SerializeField] List<EnemySO> step1Enemies;
    [SerializeField] List<EnemySO> step2Enemies;

    [SerializeField] AttackBlockedEvent attackBlockedEvent;


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartStep(TutorialStep.Step1);
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
                currentStepLogic = new Step1_Target(playerCount, spawner, NextStep, step1Enemies);
                break;

            case TutorialStep.Step2:
                currentStepLogic = new Step2_Block(playerCount, spawner, NextStep, step2Enemies);
                break;

            case TutorialStep.Step3:
                currentStepLogic = new Step3_Coop(spawner, NextStep);
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

        currentStepLogic?.OnStart();
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
        currentStepLogic?.OnAttackBlocked(blocked.Collector.ClientId);
    }

    public void OnEnemyKilled(EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    private void StartMainSimulation()
    {
        Debug.Log("Main Simulation Start");
        //応急処置
        MoveScene();
    }

    [OnInspectorButton]
    void MoveScene()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "VRSystemScene",
            LoadSceneMode.Single
        );
        Debug.Log("Move To VRSystemScene");
    }
}
