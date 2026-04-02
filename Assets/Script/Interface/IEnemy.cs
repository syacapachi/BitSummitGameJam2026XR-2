
using Unity.Netcode;

public interface IEnemy
{
    public NetworkObject NetworkObject {get;}
    public void Init(IEnemyBrokenReciever s);

}
