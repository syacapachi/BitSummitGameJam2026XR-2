using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemyShoot : GunController
{
    public EnemySO enemySO;

    Transform target;
    Coroutine shootCorutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        target = ManagerLocator.Instance.AllGameManager.protectArea.transform;
        shootCorutine = StartCoroutine(ShootCorutine());
    }


    private void Shoot()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        bullet.GetComponent<NetworkObject>().Spawn();
    }
    private IEnumerator ShootCorutine()
    {
        WaitForSeconds wait01 = new(0.1f);
        while (true)
        {
            for (float i = enemySO.shootInterval; i > 0f; i -= 0.1f)
            {
                //演出
                yield return wait01;
            }
            Shoot();
            //打った後の待機時間
            yield return wait01;
        }
    }
}