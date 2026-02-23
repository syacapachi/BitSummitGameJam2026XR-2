/* スポーン位置被るver
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;        // ゾンビ、幽霊など
    public Transform[] spawnPoints;     // 上で作ったスポーンポイント
    public int spawnCount = 3;          // フェーズごとに出す敵の数

    // フェーズ開始時に呼ぶ
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

//被らないver

using UnityEngine;
using System.Collections.Generic; // Listを使う

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;        // ゾンビ、幽霊など
    public Transform[] spawnPoints;     // スポーンポイント
    public int spawnCount = 3;          // フェーズごとに出す敵の数
    public int remain = 0;

    public void SpawnEnemies()
    {
        remain += spawnCount;

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("SpawnPointsが足りません");
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
//全撃破ボーナスver
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
    public Transform[] spawnPoints;
    public int spawnCount = 3;

    public List<GameObject> spawnedEnemies = new List<GameObject>(); // 今のフェーズの敵リスト

    public void SpawnEnemies()
    {
        spawnedEnemies.Clear(); // 前のフェーズのリストをクリア

        for (int i = 0; i < spawnCount; i++)
        {
            int enemyIndex = Random.Range(0, enemies.Length);
            int spawnIndex = Random.Range(0, spawnPoints.Length);

            GameObject e = Instantiate(enemies[enemyIndex], spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
            spawnedEnemies.Add(e); // リストに追加
        }
    }
}
*/