using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    Step1, Step2, Step3, Step4, End
}

public class TutorialManager : NetworkBehaviour
{
    [Header("Reference")]
    [SerializeField] PlayerManager playerManager;
    public NetworkVariable<TutorialStep> CurrentStep =
        new(TutorialStep.Step1);

    [SerializeField] TutorialSpawner spawner;

    TutorialBase currentStepLogic;

    [SerializeField] List<EnemySO> step1Enemies;
    [SerializeField] List<EnemySO> step2Enemies;

    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] VoidEvent OnTutorialStepCleared;
    [SerializeField] IntEvent OnTutorialStepChanged;
    private bool isInitialized = false;

    private void Start()
    {
        isInitialized = false;
        StartCoroutine(WaitForAllClientConnect());
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            attackBlockedEvent.Register(OnAttackBlocked);
            NetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
        CurrentStep.OnValueChanged += OnStepChanged;
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if(!isInitialized) 
            StartStep(TutorialStep.Step1);
    }
    IEnumerator WaitForAllClientConnect()
    {
        yield return new WaitUntil(() => IsSpawned && playerManager != null && playerManager.IsAllClientReady());
        if (!isInitialized && IsServer)
            StartStep(TutorialStep.Step1);
    }
    public override void OnNetworkDespawn()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
            attackBlockedEvent.Unregister(OnAttackBlocked);
        }
    }

    void OnStepChanged(TutorialStep oldStep, TutorialStep newStep)
    {
        StartStep(newStep);
    }

    void StartStep(TutorialStep step)
    {
        isInitialized = true;
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

        yield return new WaitForSeconds(2f);

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
