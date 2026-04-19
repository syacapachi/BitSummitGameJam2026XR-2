public interface ITutorialStep
{
    void OnStart();
    void OnEnd();

    void OnTargetDestroyed(ulong playerId);
    void OnAttackBlocked(ulong playerId);
    void OnEnemyKilled(EnemyKilled e);
}