
using Unity.Netcode;

public interface IEnemy
{
    public NetworkObject NetworkObject {get;}

    public PlayerJob EnemyJob { get; }
    /// <summary>
    /// システム的に倒せない、もしくは、死亡アニメーション中にfalseにする。
    /// </summary>
    public bool CanTakeDamage { get; }
    public void InjectSetting(EnemySO enemySO, int spawnPointIndex);

    public int SpawnPointIndexServerOnly { get; }

    //public void InitEnemyRpc(int id);
}
