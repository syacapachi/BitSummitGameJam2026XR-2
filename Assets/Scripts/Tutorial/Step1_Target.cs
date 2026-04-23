using System;
using System.Collections.Generic;

public class Step1_Target : ITutorialStep
{
    int playerCount;
    Action onComplete;
    TutorialSpawner spawner;
    List<EnemySO> step1Enemies;

    public Step1_Target(int playerCount, TutorialSpawner spawner, Action onComplete, List<EnemySO> step1Enemies)
    {
        this.playerCount = playerCount;
        this.spawner = spawner;
        this.onComplete = onComplete;
        this.step1Enemies = step1Enemies;
    }

    public void OnStart()
    {
        spawner.OnAllEnemyDead += HandleAllDead;
        spawner.SpawnTargetsForEachPlayer(step1Enemies.Count, step1Enemies);
    }

    void HandleAllDead()
    {
        onComplete?.Invoke();
    }

    public void OnEnd()
    {
        spawner.OnAllEnemyDead -= HandleAllDead;
    }

    public void OnTargetDestroyed(ulong playerId) { }
    public void OnAttackBlocked(ulong playerId) { }
    public void OnEnemyKilled(EnemyKilled e) { }
}