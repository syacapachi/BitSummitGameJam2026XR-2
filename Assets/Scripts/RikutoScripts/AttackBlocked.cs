public class AttackBlocked
{
    public IResultCollector Collector;
    public IEnemy Enemy;
}
public class AttackBlockedExpand : AttackBlocked
{
    public readonly string message;
    public AttackBlockedExpand(string message)
    {
        this.message = message;
    }
}