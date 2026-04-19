using System;
using System.Collections.Generic;

public class Step2_Block : ITutorialStep
{
    int playerCount;
    Dictionary<ulong, int> counts = new();
    Action onComplete;
    TutorialSpawner spawner;

    public Step2_Block(int playerCount, TutorialSpawner spawner, Action onComplete)
    {
        this.playerCount = playerCount;
        this.spawner = spawner;
        this.onComplete = onComplete;
    }

    public void OnStart()
    {
        counts.Clear();
        spawner.SpawnEnemiesForBlock();
    }

    public void OnEnd() { }

    public void OnAttackBlocked(ulong playerId)
    {
        if (!counts.ContainsKey(playerId))
            counts[playerId] = 0;

        counts[playerId]++;

        bool allDone = counts.Count >= playerCount;

        foreach (var c in counts.Values)
        {
            if (c < 3) allDone = false;
        }

        if (allDone)
            onComplete?.Invoke();
    }

    public void OnTargetDestroyed(ulong playerId) { }
    public void OnEnemyKilled(EnemyKilled e) { }
}