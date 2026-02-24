/* �X�|�[���ʒu���ver
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;        // �]���r�A�H��Ȃ�
    public Transform[] spawnPoints;     // ��ō�����X�|�[���|�C���g
    public int spawnCount = 3;          // �t�F�[�Y���Ƃɏo���G�̐�

    // �t�F�[�Y�J�n���ɌĂ�
    public void SpawnEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            int enemyIndex = Random.Range(0, enemies.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);

            Instantiate(
                enemies[enemyIndex],
                spawnPoints[spawnIndex].position,
                spawnPoints[spawnIndex].rotation
            );
        }
    }
}
 */

//���Ȃ�ver

using UnityEngine;
using System.Collections.Generic; // List���g��

public class NEnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;        // �]���r�A�H��Ȃ�
    public Transform[] spawnPoints;     // �X�|�[���|�C���g
    public int spawnCount = 3;          // �t�F�[�Y���Ƃɏo���G�̐�
    public int remain = 0;

    public void SpawnEnemies()
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
            );

            availablePoints.RemoveAt(spawnIndex);
        }
    }
}


/*
//�S���j�{�[�i�Xver
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
    public Transform[] spawnPoints;
    public int spawnCount = 3;

    public List<GameObject> spawnedEnemies = new List<GameObject>(); // ���̃t�F�[�Y�̓G���X�g

    public void SpawnEnemies()
    {
        spawnedEnemies.Clear(); // �O�̃t�F�[�Y�̃��X�g���N���A

        for (int i = 0; i < spawnCount; i++)
        {
            int enemyIndex = Random.Range(0, enemies.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);

            GameObject e = Instantiate(enemies[enemyIndex], spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
            spawnedEnemies.Add(e); // ���X�g�ɒǉ�
        }
    }
}
*/