using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Syacapachi.Attribute;
using Unity.Netcode.Components;

public class NEnemyShoot : GunController
{
    [SerializeField] NEnemy nEnemy;
    [SerializeField] AudioClip shotClip;
    [Header("Reference")]
    [SerializeField] NetworkAnimator networkAnimator;
    [SerializeField] Animator animator;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffect;

    private EnemyWeaponSettingsSO weaponSO;
    Coroutine shootCorutine;
    NetworkGameManager gameManager;
    PlayerManager playerManager;

    private IEnumerator Start()
    {
        //ゲームマネージャーが生成されるのを待つ
        while (ManagerLocator.Instance.AllGameManager == null || ManagerLocator.Instance.AllPlayerManager == null)
        {
            yield return null;
        }
        gameManager = ManagerLocator.Instance.AllGameManager;
        playerManager = ManagerLocator.Instance.AllPlayerManager;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        weaponSO = nEnemy.EnemySO.EnemyWeapon;
        shootCorutine = StartCoroutine(ShootCorutine());
    }
    [OnInspectorButton(showOnlyInPlayMode = true)]
    private void InspectorShoot()
    {
        shootCorutine = StartCoroutine(ShootCorutine());
    }

    protected override void OnShootServer()
    {

        if (!TryGetTarget(out var target)) return;
        //トリガーはこっち
        networkAnimator.SetTrigger("Attack");

        Vector3 direction = (target.position - transform.position).normalized;

        NetworkObject networkObject = ManagerLocator.Instance.AllNetworkObjectPool.GetNetworkObject(
            BulletPrefab,
            FirePoint.position,
            Quaternion.LookRotation(direction)
        );

        networkObject.gameObject.layer = this.gameObject.layer;

        var bullet = networkObject.GetComponent<BulletBaseController>();
        bullet.BulletInit(null, nEnemy.EnemyJob, weaponSO);

        networkObject.Spawn();
    }

    private IEnumerator ShootCorutine()
    {
        //トリガー以外はこっち
        animator.SetFloat("Speed", 2.0f);
        yield return new WaitForSeconds(weaponSO.FirstShootDelayTime);
        OnShootServer();

        while (true)
        {
            yield return new WaitForSeconds(weaponSO.reloadTime);
            OnShootServer();
        }
    }

    bool TryGetTarget(out Transform target)
    {
        target = null;
        if(gameManager == null) return false;

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
            
            bool canTarget = (nEnemy.EnemyJob & playerJob) == 0;

            //Debug.Log($"[{nameof(NEnemyShoot)}] player: {player.gameObject.name}, job: {playerJob}, enemyJob: {nEnemy.EnemyJob}, canTarget: {canTarget}");

            if (!canTarget) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player.transform;
            }
            break;
        }

        Debug.Log($"[{nameof(NEnemyShoot)},{nEnemy.EnemyJob}] nearest target: {(nearest != null ? nearest.name : "null")}");

        return nearest != null;
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
