using Unity.Netcode;
using UnityEngine;
using System;
using Syacapachi.util;
using System.Collections.Generic;



public class TutorialSpawner : NetworkBehaviour
{
    public event Action OnAllEnemyDead;

    int remain;
    bool isSpawnFinished;
    [SerializeField] EnemyKilledEvent EnemyKilled;
    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] NetworkObjectPool networkPool;
    public bool IsAllDead => remain <= 0 && isSpawnFinished;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            EnemyKilled.Register(OnEnemyKilledEvent);
            attackBlockedEvent.Register(OnAttackBlockedEvent);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            EnemyKilled.Unregister(OnEnemyKilledEvent);
            attackBlockedEvent.Unregister(OnAttackBlockedEvent);
        }
    }

    void OnEnemyKilledEvent(EnemyKilled e)
    {
        OnEnemyKilled();
    }

    void OnAttackBlockedEvent(AttackBlocked e)
    {
    ManagerLocator.Instance.TutorialManager.OnAttackBlocked(e.PlayerId);
    }

    public void SpawnTargetsForEachPlayer(int playerCount, List<EnemySO> enemyList)
    {
        if (!IsServer) return;

        if (enemyList == null || enemyList.Count == 0)
        {
            Debug.LogError("Enemy list is empty!");
            return;
        }

        remain = playerCount;
        isSpawnFinished = false;

        for (int i = 0; i < playerCount; i++)
        {
            EnemySO enemy = enemyList[i % enemyList.Count];
            SpawnTarget(i, enemy);
        }
        isSpawnFinished = true;
    }

    void SpawnTarget(int spawnIndex, EnemySO enemyData)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            Debug.LogWarning("Invalid spawn index!");
            return;
        }

        Transform point = spawnPoints[spawnIndex];

        NetworkObject obj = networkPool.GetNetworkObject(
            enemyData.Prefab,   // ←ここが重要！！
            point.position,
            Quaternion.identity
        );

        obj.Spawn();
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