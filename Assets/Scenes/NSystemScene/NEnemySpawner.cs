using UnityEngine;
using Unity.Netcode;

public class NEnemySpawner : NetworkBehaviour
{
    public Transform[] spawnPoints;

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

        GameObject obj = Instantiate(
            enemyData.prefab,
            point.position,
            point.rotation
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