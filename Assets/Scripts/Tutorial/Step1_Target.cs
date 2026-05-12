using System;
using System.Collections.Generic;

public class Step1_Target : TutorialBase
{
    readonly int playerCount;
    readonly List<EnemySO> step1Enemies;

    public Step1_Target(int playerCount, TutorialSpawner spawner, Action onComplete, List<EnemySO> step1Enemies): base(spawner, onComplete)
    {
        this.playerCount = playerCount;
        this.step1Enemies = step1Enemies;
    }

    public override void OnStart()
    {
        spawner.OnAllEnemyDead += base.HandleAllDead;
        spawner.SpawnTargetsForEachPlayer(step1Enemies.Count, step1Enemies);
    }

    public override void OnEnd()
    {
        spawner.OnAllEnemyDead -= HandleAllDead;
        spawner.KillAll();
    }

}