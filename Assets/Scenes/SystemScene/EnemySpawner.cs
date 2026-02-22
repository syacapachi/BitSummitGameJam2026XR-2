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

    public void SpawnEnemies()
    {
        // spawnPoints のコピーを作る
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("SpawnPointsが足りません");
                break;
            }

            // 敵とスポーンポイントをランダムに選ぶ
            int enemyIndex = Random.Range(0, enemies.Length);
            int spawnIndex = Random.Range(0, availablePoints.Count);

            Transform spawnPoint = availablePoints[spawnIndex];

            Instantiate(
                enemies[enemyIndex],
                spawnPoint.position,
                spawnPoint.rotation
            );

            // ここで使ったスポーンポイントは削除
            availablePoints.RemoveAt(spawnIndex);
        }
    }
}