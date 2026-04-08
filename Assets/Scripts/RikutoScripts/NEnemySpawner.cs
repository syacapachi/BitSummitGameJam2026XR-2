using Syacapachi.Attribute;
using Syacapachi.util;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NEnemySpawner : NetworkBehaviour,IEnemyBrokenReciever,ISpawnable,IKillable
{
    [SerializeField] NetworkObjectPool networkPool;
    [SerializeField] EnemyDeathReciver reciver;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Transform protectArea;
    [Header("SubScribe Event")]
    [SerializeField] EnemyKilledEvent EnemyKilled;

    private int remain;
    public int Remain => remain;
    private bool spawnFinished = false;
    public bool SpawnFinished => spawnFinished;
    private readonly List<IEnemy> enemies = new List<IEnemy>();
    /*
        public void SpawnFromPhase(PhaseSO phase)
        {
            if (!IsServer) return;

            remain = 0;

            foreach (var data in phase.spawnList)
            {
                for (int i = 0; i < data.count; i++)
                {
                    SpawnEnemy(data.enemyType, phase.usableSpawnPointIndex);
                    remain++;
                }
            }
        }
    */

    public override void OnNetworkSpawn()
    {
        EnemyKilled.Register(t => OnEnemyKilled(t.KilledEnemy));
    }
    public override void OnNetworkDespawn()
    {
        EnemyKilled.Unregister(t => OnEnemyKilled(t.KilledEnemy));
    }
    public void SpawnFromEvent(List<SpawnEvent> events)
    {
        if (!IsServer) return;

        StopAllCoroutines();
        remain = 0;

        StartCoroutine(SpawnRoutine(events));
    }

    IEnumerator SpawnRoutine(List<SpawnEvent> spawnEvents)
    {
        float timer = 0f;

        // spawnTime順にソート（重要）
        spawnEvents.Sort((a, b) => a.SpawnTime.CompareTo(b.SpawnTime));

        int index = 0;
        spawnFinished = false; // 念のためリセット


        while (index < spawnEvents.Count)
        {
            timer += Time.deltaTime;

            // 今の時間で出すべき敵を全部出す
            while (index < spawnEvents.Count && spawnEvents[index].SpawnTime <= timer)
            {
                SpawnEvent e = spawnEvents[index];

                SpawnEnemy(e.EnemyType, e.SpawnPointIndex);
                remain++;

                index++;
            }

            yield return null;
        }
        spawnFinished = true;
    }
    /*
    void SpawnEnemy(EnemySO enemyData, int[] usableIndex)
    {
        if (usableIndex.Length == 0)
        {
            Debug.LogWarning("No usable spawn points!");
            return;
        }
        int maxLength = Mathf.Min(usableIndex.Length,spawnPoints.Length);
        int randomArrayIndex = Random.Range(0, maxLength);
        int spawnIndex = usableIndex[randomArrayIndex];

        Transform point = spawnPoints[spawnIndex];

        Vector3 dir = protectArea.position - point.position;
        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject obj = Instantiate(
            enemyData.prefab,
            point.position,
            rot
        );

        obj.GetComponent<NetworkObject>().Spawn();
    }
    */

    void SpawnEnemy(EnemySO enemyData, int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            Debug.LogWarning("Invalid spawn index!");
            return;
        }

        Transform point = spawnPoints[spawnIndex];

        Vector3 dir = protectArea.position - point.position;
        Quaternion rot = Quaternion.LookRotation(dir);

        NetworkObject networkObject = networkPool.GetNetworkObject(
            enemyData.Prefab,
               point.position,
               rot);

        var enemy = networkObject.GetComponent<IEnemy>();
        enemy.Init(reciver);
        RegisterEnemy(enemy);
        networkObject.Spawn();
        
    }

    public void OnEnemyKilled(IEnemy enemy)
    {
        if (!IsServer) return;
        remain--;
        UnregisterEnemy(enemy);
    }

    public bool IsAllDead => (spawnFinished && remain <= 0);

    private void RegisterEnemy(IEnemy enemy)
    {
        enemies.Add(enemy);
    }

    private void UnregisterEnemy(IEnemy enemy)
    {
        enemies.Remove(enemy);
    }

    public void KillAll()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.NetworkObject.IsSpawned)
            {
                enemy.NetworkObject.Despawn(true);   
            }
        }
        enemies.Clear();
        remain = 0;
        spawnFinished = false;
    }

    public void ResetSpawner()
    {
        StopAllCoroutines();

        KillAll();

        remain = 0;
        spawnFinished = false;
    }
}
[GenerateEvent(typeof(GameEventSOBase<>))]
public class EnemyKilled 
{
    public IEnemy KilledEnemy;
}