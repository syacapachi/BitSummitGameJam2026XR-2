using UnityEngine;
using System.Collections.Generic; // List���g��
using Unity.Netcode;

public class NEnemySpawner : NetworkBehaviour
{
    public GameObject[] enemies;        // �]���r�A�H��Ȃ�
    public Transform[] spawnPoints;     // �X�|�[���|�C���g
    public int spawnCount = 3;          // �t�F�[�Y���Ƃɏo���G�̐�
    public int remain = 0;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    public void SpawnEnemiesRpc()
    {
        remain += spawnCount;

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("SpawnPoints������܂���");
                break;
            }

            int enemyIndex = Random.Range(0, enemies.Length);
            int spawnIndex = Random.Range(0, availablePoints.Count);

            Transform spawnPoint = availablePoints[spawnIndex];

            Instantiate(
                enemies[enemyIndex],
                spawnPoint.position,
                spawnPoint.rotation
            ).GetComponent<NetworkObject>().Spawn();

            availablePoints.RemoveAt(spawnIndex);
        }
    }
}