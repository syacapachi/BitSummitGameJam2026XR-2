using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    public EnemySO enemySO;


    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        if (reciever.GameObject == ManagerLocator.Instance.AllGameManager.protectArea)
        {
            Debug.Log("Bullet hit ProtectArea");

            ManagerLocator.Instance.AllGameManager.BulletHitProtectArea(-enemySO.Damage);

            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}