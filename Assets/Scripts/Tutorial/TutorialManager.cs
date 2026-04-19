using Syacapachi.Attribute;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    Step1,
    Step2,
    Step3,
    Step4,
    End
}

public class TutorialManager : NetworkBehaviour
{
    // --- 同期される現在ステップ ---
    public NetworkVariable<TutorialStep> CurrentStep =
        new NetworkVariable<TutorialStep>(TutorialStep.Step1);

    [SerializeField] int playerCount = 2;

    // --- イベント ---
    [SerializeField] EnemyKilledEvent killedEvent;

    // --- ステップ用データ ---
    HashSet<ulong> step1Players = new();
    Dictionary<ulong, int> step2Counts = new();
    HashSet<ulong> step3Players = new();

    // -------------------------
    // 初期化
    // -------------------------
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentStep.Value = TutorialStep.Step1;
            StartStep(CurrentStep.Value);
        }

        CurrentStep.OnValueChanged += OnStepChanged;
    }

    void OnDestroy()
    {
        CurrentStep.OnValueChanged -= OnStepChanged;
    }

    void OnStepChanged(TutorialStep oldStep, TutorialStep newStep)
    {
        Debug.Log($"Step Changed: {newStep}");
        StartStep(newStep);
    }

    // -------------------------
    // ステップ開始
    // -------------------------
    void StartStep(TutorialStep step)
    {
        Debug.Log($"Start {step}");

        switch (step)
        {
            case TutorialStep.Step1:
                step1Players.Clear();
                SpawnTargetsForEachPlayer();
                break;

            case TutorialStep.Step2:
                step2Counts.Clear();
                SpawnEnemiesForBlock();
                break;

            case TutorialStep.Step3:
                step3Players.Clear();
                SpawnEnemiesForCoop();
                break;

            case TutorialStep.Step4:
                StartMainSimulation();
                break;
        }
    }

    // -------------------------
    // ステップ遷移（Serverのみ）
    // -------------------------
    [OnInspectorButton("Next Step")]
    void NextStep()
    {
        if (!IsServer) return;

        CurrentStep.Value++;

        if (CurrentStep.Value == TutorialStep.End)
        {
            Debug.Log("Tutorial Finished!");

            MoveScene(); // ← ここで呼ぶ
            return;
        }
    }

    // -------------------------
    // Step1：的破壊（全員）
    // -------------------------
    public void OnTargetDestroyed(ulong playerId)
    {
        if (!IsServer) return;
        if (CurrentStep.Value != TutorialStep.Step1) return;

        step1Players.Add(playerId);

        if (step1Players.Count >= playerCount)
        {
            NextStep();
        }
    }

    // -------------------------
    // Step2：3回防がれる（全員）
    // -------------------------
    public void OnAttackBlocked(ulong playerId)
    {
        if (!IsServer) return;
        if (CurrentStep.Value != TutorialStep.Step2) return;

        if (!step2Counts.ContainsKey(playerId))
            step2Counts[playerId] = 0;

        step2Counts[playerId]++;

        bool allDone = true;

        foreach (var count in step2Counts.Values)
        {
            if (count < 3)
                allDone = false;
        }

        if (allDone && step2Counts.Count >= playerCount)
        {
            NextStep();
        }
    }

    // -------------------------
    // Step3：協力キル
    // -------------------------
    private void OnEnable()
    {
        if (killedEvent != null)
            killedEvent.Register(OnEnemyKilled);
    }

    private void OnDisable()
    {
        if (killedEvent != null)
            killedEvent.Unregister(OnEnemyKilled);
    }

    void OnEnemyKilled(EnemyKilled e)
    {
        if (!IsServer) return;
        if (CurrentStep.Value != TutorialStep.Step3) return;

        ulong playerId = e.KilledEnemy.LastAttackerId; ;

        step3Players.Add(playerId);

        if (step3Players.Count >= playerCount)
        {
            NextStep();
        }
    }

    // -------------------------
    // スポーン処理（中身は自由に実装）
    // -------------------------
    void SpawnTargetsForEachPlayer()
    {
        Debug.Log("Spawn Targets");
        // 各プレイヤーごとに専用ターゲットを生成
    }

    void SpawnEnemiesForBlock()
    {
        Debug.Log("Spawn Block Enemies");
        // 防御される敵生成
    }

    void SpawnEnemiesForCoop()
    {
        Debug.Log("Spawn Coop Enemies");
        // 相方用の敵生成
    }

    void StartMainSimulation()
    {
        Debug.Log("Start Main Simulation");
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