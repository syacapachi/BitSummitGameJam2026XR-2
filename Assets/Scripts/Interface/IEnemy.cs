
using Unity.Netcode;

public interface IEnemy
{
    public NetworkObject NetworkObject {get;}

    public PlayerJob EnemyJob { get; }
    /// <summary>
    /// こいつの存在意義は？
    /// </summary>
    public bool IsAttackable { get; }
    public void InjectSetting(EnemySO enemySO);

    //public void InitEnemyRpc(int id);
}
