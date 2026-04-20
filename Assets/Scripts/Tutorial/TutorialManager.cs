using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour
{
    public NetworkVariable<TutorialStep> CurrentStep =
        new NetworkVariable<TutorialStep>(TutorialStep.Step1);

    [SerializeField] int playerCount = 2;
    [SerializeField] TutorialSpawner spawner;

    ITutorialStep currentStepLogic;

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
        attackBlockedEvent.Unregister(OnAttackBlocked);
    }

    void OnStepChanged(TutorialStep oldStep, TutorialStep newStep)
    {
        StartStep(newStep);
    }

    void StartStep(TutorialStep step)
    {
        currentStepLogic?.OnEnd();

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

        if (CurrentStep.Value == TutorialStep.End)
        {
            MoveScene();
        }
    }

    // --- イベント転送 ---
    public void OnTargetDestroyed(ulong id)
    {
        if (!IsServer) return;
        currentStepLogic?.OnTargetDestroyed(id);
    }

    public void OnAttackBlocked(AttackBlocked blocked)
    {
        if (!IsServer) return;
        currentStepLogic?.OnAttackBlocked(blocked.PlayerId);
    }

    public void OnEnemyKilled(EnemyKilled e)
    {
        if (!IsServer) return;
        currentStepLogic?.OnEnemyKilled(e);
    }

    void StartMainSimulation()
    {
        Debug.Log("Main Simulation Start");
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