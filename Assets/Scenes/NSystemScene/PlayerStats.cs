using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class EnemyKillData
{
    public string enemyType;
    public int killCount;
}

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    public NetworkVariable<int> shotsFired = new NetworkVariable<int>(0);
    public NetworkVariable<int> hits = new NetworkVariable<int>(0);
    public NetworkVariable<int> shiled = new NetworkVariable<int>(0);
    public NetworkVariable<float> damageDealt = new NetworkVariable<float>(0);

    [SerializeField]
    private List<EnemyKillData> killCounts = new List<EnemyKillData>();


    // ”­ŽË
    public void AddShot()
    {
        if (!IsServer) return;
        shotsFired.Value++;
    }

    // –½’†
    public void AddHit()
    {
        if (!IsServer) return;
        hits.Value++;
    }

    // —^ƒ_ƒ[ƒW
    public void AddDamage(float damage)
    {
        if (!IsServer) return;
        damageDealt.Value += damage;
    }

    // “GŒ‚”j
    public void AddKill(string enemyType, int scoreValue)
    {
        if (!IsServer) return;

        score.Value += scoreValue;

        EnemyKillData data = killCounts.Find(x => x.enemyType == enemyType);

        if (data == null)
        {
            data = new EnemyKillData();
            data.enemyType = enemyType;
            data.killCount = 0;
            killCounts.Add(data);
        }

        data.killCount++;
    }

    public void AddShield()
    {
        if (!IsServer) return;
        shiled.Value++;
    }

    // –½’†—¦
    public float GetAccuracy()
    {
        if (shotsFired.Value == 0) return 0;
        return (float)hits.Value / shotsFired.Value;
    }
}