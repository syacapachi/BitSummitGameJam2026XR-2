using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Syacapachi.util;

public class NEnemyShoot : GunController
{
    public EnemySO enemySO;
    
    private EnemyWeaponSettingsSO weaponSO;

    Transform target;
    Coroutine shootCorutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        target = ManagerLocator.Instance.AllGameManager.protectArea.transform;
        weaponSO = enemySO.enemyWeapon;
        weaponSO ??= base.WeaponSettings as EnemyWeaponSettingsSO;
        shootCorutine = StartCoroutine(ShootCorutine());
    }

    protected override void OnShoot()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        NetworkObject networkObject = NetworkObjectPool.Singleton.GetNetworkObject(
            BulletPrefab, 
            FirePoint.position, 
            Quaternion.LookRotation(direction)
            );
        var bullet = networkObject.GetComponent<BulletBaseController>();
        bullet.BulletInit(0,PlayerJob.Nothing,weaponSO);
        networkObject.Spawn();
        
    }

    private IEnumerator ShootCorutine()
    {
        WaitForSeconds wait01 = new(0.1f);

        while (true)
        {
            for (float i = weaponSO.reloadTime; i > 0f; i -= 0.1f)
            {
                //演出
                yield return wait01;
            }

            OnShoot();

            //打った後の待機時間
            yield return wait01;
        }
    }
}