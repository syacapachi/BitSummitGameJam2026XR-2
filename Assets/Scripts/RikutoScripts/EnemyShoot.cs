using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Syacapachi.util;

public class EnemyShoot : GunController
{
    public EnemySO enemySO;
    private EnemyWeaponSettingsSO weaponSO; 

    Transform target;
    Coroutine shootCorutine;
    public PlayerJob enemyJob; // Human / Ghost

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        //target = ManagerLocator.Instance.AllGameManager.protectArea.transform;
        weaponSO = enemySO.EnemyWeapon;
        shootCorutine = StartCoroutine(ShootCorutine());
    }


    private void Shoot()
    {
        target = GetNearestTarget();

        if (target == null) return; // 対象いなければ撃たない

        Vector3 direction = (target.position - transform.position).normalized;

        NetworkObjectPool.Singleton.GetNetworkObject(
            BulletPrefab,
            FirePoint.position,
            Quaternion.LookRotation(direction)
            ).Spawn();
    }
    private IEnumerator ShootCorutine()
    {
        WaitForSeconds wait01 = new(0.1f);
        while (true)
        {
            for (float i = weaponSO.fireInterval; i > 0f; i -= 0.1f)
            {
                //演出
                yield return wait01;
            }
            Shoot();
            //打った後の待機時間
            yield return wait01;
        }
    }

    Transform GetNearestTarget()
    {
        var players = ManagerLocator.Instance.AllPlayerManager.AllPlayers;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var player in players)
        {
            var prop = player.propaty;
            if (prop == null) continue;

            var playerJob = prop.Job;

            // 敵タイプに応じたフィルタ
            bool canTarget = (enemyJob & playerJob) != 0;

            if (!canTarget) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
        }

        return nearest;
    }
}
