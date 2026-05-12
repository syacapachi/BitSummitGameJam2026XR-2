using System;

public class Step4_Coop : TutorialBase
{
    public Step4_Coop(TutorialSpawner spawner, Action onComplete) :base(spawner, onComplete)
    {
    }

    public override void OnStart()
    {
        spawner.OnAllEnemyDead += base.HandleAllDead;

        // ★すでに全滅していた場合
        if (spawner.IsAllDead)
        {
            HandleAllDead();
        }
        spawner.ApplyAttackableAfterSpawn(true);
    }

    public override void OnEnd()
    {
        spawner.OnAllEnemyDead -= base.HandleAllDead;
    }
}