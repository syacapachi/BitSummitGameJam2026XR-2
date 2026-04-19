using System;

public class Step3_Coop : ITutorialStep
{
    Action onComplete;
    TutorialSpawner spawner;

    public Step3_Coop(TutorialSpawner spawner, Action onComplete)
    {
        this.spawner = spawner;
        this.onComplete = onComplete;
    }

    public void OnStart()
    {
        spawner.SpawnEnemiesForCoop();
        spawner.OnAllEnemyDead += HandleAllDead;
    }

    public void OnEnd()
    {
        spawner.OnAllEnemyDead -= HandleAllDead;
    }

    void HandleAllDead()
    {
        onComplete?.Invoke();
    }

    public void OnTargetDestroyed(ulong playerId) { }
    public void OnAttackBlocked(ulong playerId) { }
    public void OnEnemyKilled(EnemyKilled e) { }
}