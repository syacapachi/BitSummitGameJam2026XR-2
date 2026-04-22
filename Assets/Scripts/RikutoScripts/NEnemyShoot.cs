using System.Collections;
using Unity.Netcode;
using UnityEngine;

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
        weaponSO = enemySO.EnemyWeapon;
        weaponSO ??= base.WeaponSettings as EnemyWeaponSettingsSO;
        shootCorutine = StartCoroutine(ShootCorutine());
    }

    protected override void OnShootServer()
    {
        target = GetTarget();

        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;

        NetworkObject networkObject = ManagerLocator.Instance.AllNetworkObjectPool.GetNetworkObject(
            BulletPrefab,
            FirePoint.position,
            Quaternion.LookRotation(direction)
        );

        //networkObject.gameObject.layer = this.gameObject.layer;
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer == -1)
        {
            Debug.LogWarning("[NEnemyShoot] 'EnemyBullet' layer not found!");
            networkObject.gameObject.layer = this.gameObject.layer;
        }
        else
        {
            networkObject.gameObject.layer = enemyBulletLayer;
        }

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
        var gameManager = ManagerLocator.Instance.AllGameManager;

        switch (gameManager.CurrentGameMode)
        {
            case GameMode.Protect:
                {
                    // ProtectAreaを優先
                    var protectArea = gameManager.ProtectArea;
                    if (protectArea != null)
                        return protectArea.transform;

                    // 念のためフォールバック
                    return GetNearestPlayer();
                }

            case GameMode.Survival:
            default:
                {
                    // 常にプレイヤーのみ
                    return GetNearestPlayer();
                }
        }
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

            // [追加] デバッグ用：ターゲット判定の状況を出力
            Debug.Log($"[NEnemyShoot] player: {player.gameObject.name}, job: {playerJob}, enemyJob: {enemyJob}, canTarget: {canTarget}");

            if (!canTarget) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
        }

        // [追加] デバッグ用：最終的なターゲットを出力
        Debug.Log($"[NEnemyShoot] nearest target: {(nearest != null ? nearest.name : "null")}");

        return nearest;
    }
}
