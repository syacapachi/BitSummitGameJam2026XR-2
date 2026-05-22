using System;
using System.Collections.Generic;

public class Step2_Block : TutorialBase
{
    readonly int playerCount;
    readonly Dictionary<ulong, int> counts = new();

    private readonly List<EnemySO> step2Enemies;

    public Step2_Block(int playerCount, TutorialSpawner spawner, Action onComplete, List<EnemySO> step2Enemies):base(spawner, onComplete)
    {
        this.playerCount = playerCount;
        this.step2Enemies = step2Enemies;
    }

    public override void OnStart()
    {
        counts.Clear();
        spawner.SpawnTargetsForEachPlayer(playerCount, step2Enemies);
        // 敵をスポーン
        spawner.ApplyAttackableAfterSpawn(false);

    }


    public override void OnEnd()
    {
    }

    // ★ここがメインロジック
    public override void OnAttackBlocked(ulong playerId)
    {
        if (!counts.ContainsKey(playerId))
            counts[playerId] = 0;

        counts[playerId]++;

        // デバッグ（おすすめ）
        UnityEngine.Debug.Log($"Player {playerId} Block Count: {counts[playerId]}");

        bool allDone = counts.Count >= playerCount;

        foreach (int count in counts.Values)
        {
            if (count < 1)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            UnityEngine.Debug.Log("Step2 Complete!");
            onComplete?.Invoke();
        }
    }
}