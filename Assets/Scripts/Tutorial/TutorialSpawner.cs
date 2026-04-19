using Unity.Netcode;
using UnityEngine;
using System;

public class TutorialSpawner : NetworkBehaviour
{
    public event Action OnAllEnemyDead;

    int remain;
    bool isSpawnFinished;

    public void SpawnTargetsForEachPlayer()
    {
        if (!IsServer) return;
        Debug.Log("Spawn Targets");
    }

    public void SpawnEnemiesForBlock()
    {
        if (!IsServer) return;
        Debug.Log("Spawn Block Enemies");
    }

    public void SpawnEnemiesForCoop()
    {
        if (!IsServer) return;
        Debug.Log("Spawn Coop Enemies");
        remain = 3; // ‰¼
        isSpawnFinished = true;
    }

    public void OnEnemyKilled()
    {
        if (!IsServer) return;

        remain--;

        if (remain <= 0 && isSpawnFinished)
        {
            OnAllEnemyDead?.Invoke();
        }
    }
}