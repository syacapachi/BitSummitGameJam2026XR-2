using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NEnemyShoot : GunController
{
    public EnemySO enemySO;
    [SerializeField] NEnemy nEnemy;
    private EnemyWeaponSettingsSO weaponSO;
    [SerializeField] AudioClip shotClip;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffect;

    Transform target;
    Coroutine shootCorutine;

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

        networkObject.gameObject.layer = this.gameObject.layer;

        var bullet = networkObject.GetComponent<BulletBaseController>();
        bullet.BulletInit(null, PlayerJob.Nothing, weaponSO);

        networkObject.Spawn();
    }

    private IEnumerator ShootCorutine()
    {
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
        if(gameManager == null) return transform;

        switch (gameManager.CurrentGameMode)
        {
            case GameMode.Protect:
                {
                    var protectArea = gameManager.ProtectArea;
                    if (protectArea != null)
                        return protectArea.transform;
                    return GetNearestPlayer();
                }

            case GameMode.Survival:
            default:
                {
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

            bool canTarget = (nEnemy.EnemyJob & playerJob) == 0;

            Debug.Log($"[{nameof(NEnemyShoot)}] player: {player.gameObject.name}, job: {playerJob}, enemyJob: {nEnemy.EnemyJob}, canTarget: {canTarget}");

            if (!canTarget) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
            break;
        }

        Debug.Log($"[{nameof(NEnemyShoot)}] nearest target: {(nearest != null ? nearest.name : "null")}");

        return nearest;
    }
    public override void PlayShotSound()
    {
        gameEffect.Invoke(new GameEffect(shotClip, null, transform.position));
    }
#if UNITY_EDITOR
    private void Reset()
    {
        nEnemy ??= GetComponent<NEnemy>();   
    }
#endif
}
