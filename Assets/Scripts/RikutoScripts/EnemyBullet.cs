using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : BulletBaseController
{
    public EnemySO enemySO;
    float speed;
    public PlayerJob bulletTargetJob;

    void DespawnBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        ManagerLocator.Instance.AllGameManager.BulletHitProtectArea((int)Damage);
        DespawnBullet();
    }
}