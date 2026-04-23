
using Unity.Netcode;

public interface IEnemy
{
    public int Layer { get; }
    public NetworkObject NetworkObject {get;}

    public PlayerJob EnemyJob { get; }
    /// <summary>
    /// こいつの存在意義は？
    /// </summary>
    public bool IsAttackable { get; }
}
