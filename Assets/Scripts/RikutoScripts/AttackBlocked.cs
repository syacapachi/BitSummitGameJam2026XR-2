public readonly struct AttackBlocked
{
    public readonly IResultCollector Collector;
    public readonly IEnemy Enemy;
    public AttackBlocked(IResultCollector collector, IEnemy enemy)
    {
        this.Collector = collector;
        this.Enemy = enemy;
    }
}