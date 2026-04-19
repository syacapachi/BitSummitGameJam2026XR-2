using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartStep(TutorialStep.Step1);
        }

        CurrentStep.OnValueChanged += OnStepChanged;
    }

    void OnDestroy()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
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
                currentStepLogic = new Step1_Target(playerCount, spawner, NextStep);
                break;

            case TutorialStep.Step2:
                currentStepLogic = new Step2_Block(playerCount, spawner, NextStep);
                break;

            case TutorialStep.Step3:
                currentStepLogic = new Step3_Coop(spawner, NextStep);
                break;

            case TutorialStep.Step4:
                StartMainSimulation();
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

    public void OnAttackBlocked(ulong id)
    {
        if (!IsServer) return;
        currentStepLogic?.OnAttackBlocked(id);
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

    void MoveScene()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "VRSystemScene",
            LoadSceneMode.Single
        );
    }
}