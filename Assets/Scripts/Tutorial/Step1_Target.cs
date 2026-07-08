using System;
using System.Collections.Generic;

public class Step1_Target : TutorialBase
{
    readonly int playerCount;
    /// <summary>
    /// スポーンする敵のリストかArray。プレイヤーごとに1体ずつスポーンするため、プレイヤー数と同数必要。
    /// </summary>
    readonly EnemySO step1Enemy;
    readonly int spawnCount;
    public Step1_Target(int playerCount, TutorialSpawner spawner, Action onComplete, EnemySO step1Enemy, int spawnCount = 7): base(spawner, onComplete)
    {
        this.playerCount = playerCount;
        this.step1Enemy = step1Enemy;
        this.spawnCount = spawnCount;
    }

    public sealed override void OnStart()
    {
        spawner.OnAllEnemyDead += base.HandleAllDead;
        spawner.SpawnTargetServerOnly(step1Enemy,spawnCount);
    }

    public sealed override void OnEnd()
    {
        spawner.OnAllEnemyDead -= HandleAllDead;
        spawner.KillAll();
    }

}