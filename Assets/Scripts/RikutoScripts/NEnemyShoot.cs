using Syacapachi.Attribute;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NEnemyShoot : GunController
{
    [Header("LocalBullet")]
    [SerializeField] GameObject localBulletPrefab;
    [SerializeField] NEnemy nEnemy;
    [SerializeField] AudioEffectData shotAudioEffect;
    [Header("Reference")]
    [SerializeField] NetworkAnimator networkAnimator;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffect;

    private EnemyWeaponSettingsSO weaponSORpc;
    Coroutine shootCorutine;
    NetworkGameManager gameManager;
    PlayerManager playerManager;
    float remainServerOnly = 0;
    Transform nearestPlayerTransfrom = null;
    private void Start()
    {
        gameManager = ManagerLocator.Instance.AllGameManager;
        playerManager = ManagerLocator.Instance.AllPlayerManager;
    }

    public override void OnNetworkSpawn()
    {
        weaponSORpc = nEnemy.EnemyWeaponRpc != null ? nEnemy.EnemyWeaponRpc : (EnemyWeaponSettingsSO)base.WeaponSettings;
        if (!IsServer) return;
        remainServerOnly = weaponSORpc.maxAmmo;
        if (nEnemy.CanAttackRpc)
        {
            shootCorutine = StartCoroutine(ShootCorutine());
        }
    }
    [OnInspectorButton(ShowOnlyInPlayMode = true)]
    private void InspectorShoot()
    {
        shootCorutine = StartCoroutine(ShootCorutine());
    }
    public void ShootFromAnimationEvent()
    {
        if(!TryGetTarget(out nearestPlayerTransfrom)) return;
        ShootLocal();
        if (!IsServer) return;
        OnShootServer();
    }
    private void ShootLocal()
    {
        
        Vector3 direction = (nearestPlayerTransfrom.position - FirePoint.position).normalized;

        GameObject bulletObject = ManagerLocator.Instance.LocalObjectPool.Get(
            localBulletPrefab
        );
        bulletObject.transform.SetPositionAndRotation(
            FirePoint.position,
            Quaternion.LookRotation(direction)
            );

        bulletObject.layer = this.gameObject.layer;
        if (bulletObject.TryGetComponent<LocalBullet>(out var localBullet))
        {
            localBullet.BulletInit(weaponSORpc.bulletSetting);
        }
    }
    protected override void OnShootServer()
    {
        if (nearestPlayerTransfrom == null) return;
        Vector3 direction = (nearestPlayerTransfrom.position - FirePoint.position).normalized;

        NetworkObject networkObject = ManagerLocator.Instance.AllNetworkObjectPool.GetNetworkObject(
            BulletPrefab,
            FirePoint.position,
            Quaternion.LookRotation(direction)
        );

        networkObject.gameObject.layer = this.gameObject.layer;

        var bullet = networkObject.GetComponent<BulletBaseController>();
        bullet.BulletInit(null, nEnemy.EnemyJob, weaponSORpc.bulletSetting);

        networkObject.Spawn();
    }

    private IEnumerator ShootCorutine()
    {
        //トリガー以外はこっち
        //networkAnimator?.Animator.SetFloat("Speed", 2.0f);
        WaitForSeconds waitInterval = new WaitForSeconds(weaponSORpc.fireInterval);
        WaitForSeconds waitReload = new WaitForSeconds(weaponSORpc.reloadTime);
        yield return new WaitForSeconds(weaponSORpc.FirstShootDelayTime);
        //トリガーはこっち
        networkAnimator?.SetTrigger("Attack");

        while (true)
        {
            yield return waitInterval;
            //トリガーはこっち
            networkAnimator?.SetTrigger("Attack");
            if (--remainServerOnly <= 0)
            {
                yield return waitReload;
                remainServerOnly = weaponSORpc.maxAmmo;
            }
        }
    }

    bool TryGetTarget(out Transform target)
    {
        target = null;
        if (gameManager == null) return false;

        switch (gameManager.CurrentGameMode)
        {
            case GameMode.Protect:
                {
                    var protectArea = gameManager.ProtectArea;
                    if (protectArea != null)
                    {
                        target = protectArea.transform;
                        return true;
                    }
                    return TryGetNearestPlayer(out target);
                }

            case GameMode.Survival:
            default:
                {
                    return TryGetNearestPlayer(out target);
                }
        }
    }

    bool TryGetNearestPlayer(out Transform nearest)
    {
        var players = playerManager.AllPlayers;

        nearest = null;
        float minDist = float.MaxValue;

        foreach (var player in players)
        {
            var prop = player.propaty;
            if (prop == null) continue;

            var playerJob = prop.Job;

            bool canTarget = nEnemy.IsAttackableJob(playerJob);

            if (!canTarget) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
            break;
        }

        Debug.Log($"[{nameof(NEnemyShoot)},{nEnemy.EnemyJob}] nearest target: {(nearest != null ? nearest.name : "null")}", gameObject);

        return nearest != null;
    }
    public override void PlayShotSound()
    {
        gameEffect.Invoke(new GameEffect(shotAudioEffect.ToRuntimeData(), transform.position));
        //gameEffect.Invoke(GameEffect.CreateAudioEffect(shotClip, transform.position));
    }
#if UNITY_EDITOR
    private void Reset()
    {
        nEnemy ??= GetComponent<NEnemy>();
    }
#endif
}