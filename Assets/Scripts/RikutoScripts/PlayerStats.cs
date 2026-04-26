using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour,IResultCollector
{
    public int score = 0;
    public int shotsFired = 0;
    public int hits = 0;
    public int shield = 0;
    public float damageDealt = 0;

    [SerializeField] EnemyDataBase enemyDataBase; // 敵のデータベースへの参照
    [SerializeField]
    private int[] killCounts;

    public ulong ClientId => OwnerClientId;

    void Awake()
    {
        killCounts = new int[enemyDataBase.Length];
    }

    // 発射
    public void AddShot()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        shotsFired++;
    }

    // 命中
    public void AddHit()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        hits++;
    }

    // 与ダメージ
    public void AddDamage(float damage)
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        damageDealt += damage;
    }

    // 敵撃破（enemyIdに変更）
    public void AddKill(EnemySO enemyso, int scoreValue)
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        int enemyId = enemyDataBase.GetIdFromEnemyData(enemyso);
        score += scoreValue;

        if (enemyId < 0 || enemyId >= killCounts.Length)
        {
            Debug.LogWarning($"Invalid enemyso: {enemyso}");
            return;
        }

        killCounts[enemyId]++;
    }

    public void AddShield()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        shield++;
    }

    // 命中率
    public float GetAccuracy()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        if (shotsFired == 0) return 0;
        return (float)hits / shotsFired;
    }

    // 外部から取得用（重要）
    public int[] GetKillCounts()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        return killCounts;
    }

    public PlayerResultData CreateResultData()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        return new PlayerResultData
        {
            clientId = GetComponentInParent<NetworkObject>().OwnerClientId,
            score = score,
            shotsFired = shotsFired,
            hits = hits,
            shield = shield,
            damageDealt = damageDealt,
            killCounts = (int[])killCounts.Clone() // 重要：コピー
        };
    }
}