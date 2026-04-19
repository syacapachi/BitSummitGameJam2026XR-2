using System;
using System.Collections.Generic;

public class Step1_Target : ITutorialStep
{
    int playerCount;
    HashSet<ulong> cleared = new();
    Action onComplete;
    TutorialSpawner spawner;

    public Step1_Target(int playerCount, TutorialSpawner spawner, Action onComplete)
    {
        this.playerCount = playerCount;
        this.spawner = spawner;
        this.onComplete = onComplete;
    }

    public void OnStart()
    {
        cleared.Clear();
        spawner.SpawnTargetsForEachPlayer();
    }

    public void OnEnd() { }

    public void OnTargetDestroyed(ulong playerId)
    {
        cleared.Add(playerId);

        if (cleared.Count >= playerCount)
            onComplete?.Invoke();
    }

    public void OnAttackBlocked(ulong playerId) { }
    public void OnEnemyKilled(EnemyKilled e) { }
}