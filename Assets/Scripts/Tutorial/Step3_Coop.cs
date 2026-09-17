/*
using System;

public class Step3_Coop : TutorialBase
{
    public Step3_Coop(TutorialSpawner spawner, Action onComplete) :base(spawner, onComplete)
    {
    }

    public sealed override void OnStart()
    {
        spawner.OnAllEnemyDead += base.HandleAllDead;

        // ★すでに全滅していた場合
        if (spawner.IsAllDead)
        {
            HandleAllDead();
        }
        spawner.ApplyAttackableAfterSpawn(true);
    }

    public sealed override void OnEnd()
    {
        spawner.OnAllEnemyDead -= base.HandleAllDead;
    }
}
*/

using System;

public class Step3_Coop : TutorialBase
{
    public Step3_Coop(
        TutorialSpawner spawner,
        Action onComplete)
        : base(spawner, onComplete)
    {
    }

    public override void OnStart()
    {
        spawner.ApplyAttackableAfterSpawn(true);
    }

    public override void OnEnd()
    {
    }
}