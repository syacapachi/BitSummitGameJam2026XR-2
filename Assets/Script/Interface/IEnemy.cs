
using Unity.Netcode;

public interface IEnemy
{
    public int Layer { get; }
    public NetworkObject NetworkObject {get;}
    public void Init(IEnemyBrokenReciever s);

}
