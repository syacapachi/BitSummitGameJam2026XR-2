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
    public PlayerJob enemyJob;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        if(ManagerLocator.Instance.AllGameManager.ProtectArea!= null) target = ManagerLocator.Instance.AllGameManager.ProtectArea.transform;
        weaponSO = enemySO.EnemyWeapon;
        weaponSO ??= base.WeaponSettings as EnemyWeaponSettingsSO;
        shootCorutine = StartCoroutine(ShootCorutine());
    }

    protected override void OnShootServer()
    {

        target = GetTarget();
 
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;

        NetworkObject networkObject = NetworkObjectPool.Singleton.GetNetworkObject(
            BulletPrefab,
            FirePoint.position,
            Quaternion.LookRotation(direction)
        );

        networkObject.gameObject.layer = this.gameObject.layer;

        var bullet = networkObject.GetComponent<BulletBaseController>();
        bullet.BulletInit(0, PlayerJob.Nothing, weaponSO);

        networkObject.Spawn();
    }

    private IEnumerator ShootCorutine()
    {
        // 初弾
        yield return new WaitForSeconds(weaponSO.FirstShootDelayTime);
        OnShootServer();

        while (true)
        {
            yield return new WaitForSeconds(weaponSO.reloadTime);
            OnShootServer();
        }
    }

    Transform GetTarget()
    {
        // ProtectAreaが存在すれば優先
        var protectArea = ManagerLocator.Instance.AllGameManager.ProtectArea;

        if (protectArea != null)
            return protectArea.transform;

        // 無ければプレイヤーを狙う
        return GetNearestPlayer();
    }

    Transform GetNearestPlayer()
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
            bool canTarget = (enemyJob & playerJob) == 0;

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