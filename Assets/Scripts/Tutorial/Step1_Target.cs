using System;
using System.Collections.Generic;

public class Step1_Target : TutorialBase
{
    readonly int playerCount;
    /// <summary>
    /// スポーンする敵のリストかArray。プレイヤーごとに1体ずつスポーンするため、プレイヤー数と同数必要。
    /// </summary>
    readonly IReadOnlyList<EnemySO> step1Enemies;

    public Step1_Target(int playerCount, TutorialSpawner spawner, Action onComplete, IReadOnlyList<EnemySO> step1Enemies): base(spawner, onComplete)
    {
        this.playerCount = playerCount;
        this.step1Enemies = step1Enemies;
    }

    public sealed override void OnStart()
    {
        spawner.OnAllEnemyDead += base.HandleAllDead;
        spawner.SpawnTargetsForEachPlayer(step1Enemies.Count, step1Enemies);
    }

    public sealed override void OnEnd()
    {
        spawner.OnAllEnemyDead -= HandleAllDead;
        spawner.KillAll();
    }

}