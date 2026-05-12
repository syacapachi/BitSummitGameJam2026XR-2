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
    bool CanRecord =>
    ManagerLocator.Instance != null &&
    ManagerLocator.Instance.GameStateManager != null &&
    ManagerLocator.Instance.GameStateManager.CurrentGameState
        == GameState.Playing;

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
            return;
        }
        if (!CanRecord)
        {
            return;
        }
        shotsFired++;
    }

    // 命中
    public void AddHit()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return;
        }
        if (!CanRecord)
        {
            return;
        }
        hits++;
    }

    // 与ダメージ
    public void AddDamage(float damage)
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return;
        }
        if (!CanRecord)
        {
            return;
        }
        damageDealt += damage;
    }

    // 敵撃破（enemyIdに変更）
    public void AddKill(EnemySO enemyso, int scoreValue)
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return;
        }
        if (!CanRecord)
        {
            return;
        }
        int enemyId = enemyDataBase.GetIdFromEnemyData(enemyso);
        Debug.Log($"Add kill{enemyso.name}({enemyId})");
        score += scoreValue;

        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return;
        }
        if (!CanRecord)
        {
            return;
        }

        killCounts[enemyId]++;
    }

    public void AddShield()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return;
        }
        if (!CanRecord)
        {
            return;
        }
        shield++;
    }

    // 命中率
    public float GetAccuracy()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
            return 0;
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

    public PlayerResultData CreateResultDataServerOnly()
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        return new PlayerResultData
        {
            clientId = OwnerClientId,
            score = score,
            shotsFired = shotsFired,
            hits = hits,
            shield = shield,
            damageDealt = damageDealt,
            killCounts = (int[])killCounts.Clone() // 重要：コピー
        };
    }

    public void ResetStats()
    {
        score = 0;
        shotsFired = 0;
        hits = 0;
        shield = 0;
        damageDealt = 0;

        for (int i = 0; i < killCounts.Length; i++)
        {
            killCounts[i] = 0;
        }
    }
}