using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.Rendering.DebugUI;

public class PlayerStats : NetworkBehaviour,IResultCollector
{
    public int score = 0;
    public int shotsFired = 0;
    public int hits = 0;
    public int shield = 0;
    public float damageDealt = 0;

    [SerializeField]
    private int enemyTypeCount = 10; // 敵の種類数（固定）
    [SerializeField]
    private int[] killCounts;

    public ulong ClientId => OwnerClientId;

    void Awake()
    {
        killCounts = new int[enemyTypeCount];
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
    public void AddKill(int enemyId, int scoreValue)
    {
        if (!IsServer)
        {
            Debug.Log("Can call only Server");
        }
        score += scoreValue;

        if (enemyId < 0 || enemyId >= killCounts.Length)
        {
            Debug.LogWarning($"Invalid enemyId: {enemyId}");
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