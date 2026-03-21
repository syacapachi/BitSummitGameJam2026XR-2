using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NEnemySpawner : NetworkBehaviour
{
    public Transform[] spawnPoints;
    public Transform protectArea;

    private int remain;
    public int Remain => remain;
    private bool spawnFinished = false;
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

    public void SpawnFromPhase(PhaseSO phase)
    {
        if (!IsServer) return;

        StopAllCoroutines();
        remain = 0;

        StartCoroutine(SpawnRoutine(phase));
    }

    IEnumerator SpawnRoutine(PhaseSO phase)
    {
        float timer = 0f;

        // spawnTime順にソート（重要）
        var events = new List<SpawnEvent>(phase.spawnEvents);
        events.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        int index = 0;
        spawnFinished = false; // 念のためリセット


        while (timer < phase.phaseTime)
        {
            timer += Time.deltaTime;

            // 今の時間で出すべき敵を全部出す
            while (index < events.Count && events[index].spawnTime <= timer)
            {
                SpawnEvent e = events[index];

                SpawnEnemy(e.enemyType, e.spawnPointIndex);
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

    GameObject obj = Instantiate(
        enemyData.prefab,
        point.position,
        rot
    );

    obj.GetComponent<NetworkObject>().Spawn();
}

    public void EnemyKilled()
    {
        if (!IsServer) return;
        remain--;
    }

    public bool AllDead()
    {
        return remain <= 0;
    }
}