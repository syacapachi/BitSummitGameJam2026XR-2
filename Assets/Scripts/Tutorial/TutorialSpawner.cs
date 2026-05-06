using Syacapachi.util;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



public class TutorialSpawner : NetworkBehaviour
{
    public event Action OnAllEnemyDead;

    int remain;
    bool isSpawnFinished;
    [Header("Subscribe Event")]
    [SerializeField] EnemyKilledEvent EnemyKilled;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] NetworkObjectPool networkPool;
    public bool IsAllDead => remain <= 0 && isSpawnFinished;
    private readonly List<IEnemy> spawnedEnemies = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            EnemyKilled.Register(OnEnemyKilledEvent);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            EnemyKilled.Unregister(OnEnemyKilledEvent);
        }
    }

    void OnEnemyKilledEvent(EnemyKilled e)
    {
        OnEnemyKilled();

        spawnedEnemies.Remove(e.KilledEnemy);

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
            EnemySO enemy = enemyList[i];
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

        if (obj.TryGetComponent<IEnemy>(out var enemy))
        {
            enemy.InjectSetting(enemyData,spawnIndex);
            spawnedEnemies.Add(enemy);
        }
        obj.Spawn(true);
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

    public void KillAll()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.NetworkObject.IsSpawned)
            {
                enemy.NetworkObject.Despawn(true);
            }
        }

        spawnedEnemies.Clear();
        remain = 0;
        isSpawnFinished = false;
    }

    public void ApplyAttackableAfterSpawn(bool value)
    {
        StartCoroutine(CoApplyAttackable(value));
    }

    private IEnumerator CoApplyAttackable(bool value)
    {
        yield return null;

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null || enemy is not NEnemy nenemy) continue;

            nenemy.SetAttackabe(value);
        }
    }
}