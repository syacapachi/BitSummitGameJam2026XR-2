using System;
using System.Collections.Generic;

public class Step2_Block : ITutorialStep
{
    int playerCount;
    Dictionary<ulong, int> counts = new();
    Action onComplete;
    TutorialSpawner spawner;

    List<EnemySO> step2Enemies;

    public Step2_Block(int playerCount, TutorialSpawner spawner, Action onComplete, List<EnemySO> step2Enemies)
    {
        this.playerCount = playerCount;
        this.spawner = spawner;
        this.onComplete = onComplete;
        this.step2Enemies = step2Enemies;
    }

    public void OnStart()
    {
        counts.Clear();

        // 敵をスポーン
        spawner.SpawnTargetsForEachPlayer(playerCount, step2Enemies);
    }

    public void OnEnd()
    {
    }

    // ★ここがメインロジック
    public void OnAttackBlocked(ulong playerId)
    {
        if (!counts.ContainsKey(playerId))
            counts[playerId] = 0;

        counts[playerId]++;

        // デバッグ（おすすめ）
        UnityEngine.Debug.Log($"Player {playerId} Block Count: {counts[playerId]}");

        bool allDone = counts.Count >= playerCount;

        foreach (var c in counts.Values)
        {
            if (c < 3)
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

    public void OnTargetDestroyed(ulong playerId) { }
    public void OnEnemyKilled(EnemyKilled e) { }
}