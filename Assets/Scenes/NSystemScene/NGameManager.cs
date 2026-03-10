using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class NGameManager : NetworkBehaviour
{
    public static NGameManager Instance;
    public PhaseSO[] phases;
    private int currentPhaseIndex = -1;
    private float timer;

    public NEnemySpawner spawner; 
    private int score = 0;

    public GameObject protectArea;
    private bool isEnemycome = false;

    public NetworkVariable<int> syncedPhaseIndex = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> IsGameFinished = new NetworkVariable<bool>(false);
    bool gameStarted = false;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        syncedPhaseIndex.OnValueChanged += OnPhaseChanged;

        if (!IsServer) return;

        spawner = GetComponentInChildren<NEnemySpawner>();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("Game Start");
        gameStarted = true;

        ResetAndStartGame();
    }

    void Update()
    {
        if (!IsServer) return;
        if (currentPhaseIndex >= phases.Length) return;
        if (!gameStarted) return;

        timer -= Time.deltaTime;

        if (timer <= 0 || spawner.AllDead())
        {
            EndPhase();
        }
    }
    [OnInspectorButton("Reset And Start Game")]
    public void ResetAndStartGame()
    {
        currentPhaseIndex = -1;
        StartNextPhase();
    }

    void StartNextPhase()
    {
        currentPhaseIndex++;

        if (currentPhaseIndex >= phases.Length)
        {
            Debug.Log("GAME CLEAR");
            if (IsServer)
            {
                Debug.Log("Game Finished");
                IsGameFinished.Value = true;
            }

            return;
        }

        syncedPhaseIndex.Value = currentPhaseIndex;

        PhaseSO phase = phases[currentPhaseIndex];
        timer = phase.phaseTime;

        spawner.SpawnFromPhase(phase);

        Debug.Log("Start Phase: " + currentPhaseIndex);
    }

    void EndPhase()
    {
        PhaseSO phase = phases[currentPhaseIndex];

        if (spawner.AllDead() && !isEnemycome)
        {
            Debug.Log("Clear Bonus");
            AddScore(phase.clearBonus);
        }

        StartNextPhase();
    }

    public void EnemyKilled(int scoreValue)
    {
        AddScore(scoreValue);
        spawner.EnemyKilled();
    }

    public void AddScore(int value)
    {
        score += value;
        if(value>0) Debug.Log("Add Score");
        else Debug.Log("Subtract Score");
        Debug.Log("Score: " + score);
    }

    public void Enemycome()
    {
        isEnemycome = true;
    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        if (newValue < 0 || newValue >= phases.Length) return;

        PhaseSO phase = phases[newValue];

    }

    public int GetScore()
    {
        return score;
    }
}
