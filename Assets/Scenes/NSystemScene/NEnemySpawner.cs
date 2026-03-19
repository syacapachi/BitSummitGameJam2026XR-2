using UnityEngine;
using Unity.Netcode;

public class NEnemySpawner : NetworkBehaviour
{
    public Transform[] spawnPoints;
    public Transform dirArea;

    private int remain;
    public int Remain => remain;

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

        Vector3 dir = dirArea.position - point.position;
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