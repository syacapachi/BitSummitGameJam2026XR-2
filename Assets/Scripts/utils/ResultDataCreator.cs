using Syacapachi.Data;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ResultDataCreator : NetworkBehaviour
{
    [SerializeField] PlayerManager PlayerManager;
    [Header("Publish Event")]
    [SerializeField] ResultDataEvent resultDataRpcEvent;
    private readonly List<PlayerResultData> playerResultDataCacheList = new();

    public readonly struct ResultHeaderData
    {
        public readonly bool IsGameOver { get; }
        public readonly int Seed { get; }
        public readonly int TotalScore { get; }
        public readonly int TotalBonus { get; }
        public readonly Difficulty Difficulty { get; }
        public ResultHeaderData(bool isGameOver, int seed, int totalScore, int totalBonus, Difficulty difficulty)
        {
            IsGameOver = isGameOver;
            Seed = seed;
            TotalScore = totalScore;
            TotalBonus = totalBonus;
            Difficulty = difficulty;
        }
        public readonly override string ToString()
        {
            return $"Seed[{Seed}], isGameOver[{IsGameOver}], TotalScore[{TotalScore}], TotalBonus[{TotalBonus}], Difficulty[{Difficulty}]";
        }
    }
    public void CreateAndSendResultData(in ResultHeaderData headerData)
    {
        playerResultDataCacheList.Clear();

        foreach (var player in PlayerManager.AllPlayers)
        {
            if (player == null) continue;

            var stats = player.stats;
            if (stats == null) continue;

            playerResultDataCacheList.Add(stats.CreateResultDataServerOnly());
        }
        PlayerResultData[] datas = playerResultDataCacheList.ToArray();
        float cooporate = ResultDataCreator.CalculateCooperation(datas.AsSpan());
        ResultData data = new ResultData()
        {
            DateTime = DateTime.Now.ToString(),
            RemainHP = headerData.TotalScore,
            TotalBonusHP = headerData.TotalBonus,
            Cooperation = cooporate,
            IsGameOver = headerData.IsGameOver,
            GameSeed = headerData.Seed,
            Difficulty = headerData.Difficulty,
            detail = datas
        };
        OnSendResultRpc(data);
    }
    [Rpc(SendTo.ClientsAndHost)]
    void OnSendResultRpc(ResultData result)
    {
        resultDataRpcEvent.Invoke(result);
    }
    static float CalculateCooperation(in ReadOnlySpan<PlayerResultData> results)
    {
        float totalShots = 0;
        float totalHits = 0;
        float totalKills = 0;
        float totalShield = 0;

        foreach (var r in results)
        {
            totalShots += r.shotsFired;
            totalHits += r.hits;
            totalShield += r.shield;

            foreach (var k in r.killCounts)
            {
                totalKills += k;
            }
        }

        float accuracy = totalHits / Mathf.Max(1, totalShots);
        float killEfficiency = totalKills / Mathf.Max(1, totalHits);
        float waste = totalShield / Mathf.Max(1, totalShots);

        float cooperation =
            (accuracy * 0.5f +
             killEfficiency * 0.5f
             - waste * 0.2f) * 200f;

        return Mathf.Clamp(cooperation, 0f, 200f);
    }
}
