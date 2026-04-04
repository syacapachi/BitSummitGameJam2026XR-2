using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        if (reciever.GameObject == ManagerLocator.Instance.AllGameManager.ProtectArea)
        {
            Debug.Log("Bullet hit ProtectArea");
            Debug.Log($"Damage: {Damage}");

            ManagerLocator.Instance.AllGameManager.BulletHitProtectArea((int)-Damage);

            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}