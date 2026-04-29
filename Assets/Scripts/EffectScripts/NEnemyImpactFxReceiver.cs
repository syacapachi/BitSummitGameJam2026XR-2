// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;

// public class NEnemyImpactFxReceiver : NetworkBehaviour
// {
//     [SerializeField] NEnemy nEnemy;
//     [Header("Valid Hit FX / SFX")]
//     [SerializeField] private GameObject validHitFxPrefabAll;
//     [SerializeField] private AudioClip validHitSfxAll;
//     [SerializeField, Range(0f, 1f)] private float validHitVolumeAll = 1f;

//     [Header("Invalid Hit FX / SFX")]
//     [SerializeField] private GameObject invalidHitFxPrefabAll;
//     [SerializeField] private AudioClip invalidHitSfxAll;
//     [SerializeField, Range(0f, 1f)] private float invalidHitVolumeAll = 1f;

//     [Header("Settting")]
//     [SerializeField] JobSettingGenerator JobSetting;
//     [Header("Publish Event")]
//     [SerializeField] GameEffectEvent gameEffectEvent;


//     private void OnTriggerEnter(Collider other)
//     {
//         if (!IsServer) return;
//         TryHandleBulletHitServer(other);
//     }

//     private void OnCollisionEnter(Collision collision)
//     {
//         if (!IsServer) return;
//         TryHandleBulletHitServer(collision.collider);
//     }

//     private void TryHandleBulletHitServer(Collider other)
//     {
//         IDamageSender damegeSender = other.GetComponent<IDamageSender>() ?? other.GetComponentInParent<IDamageSender>();
//         if (damegeSender == null) return;
//         if (damegeSender is not BulletBaseController bullet) return;
//         Vector3 fxPosition = other.ClosestPoint(transform.position);
//         if (fxPosition == Vector3.zero)
//         {
//             fxPosition = other.transform.position;
//         }
//         PlayerJob shooterJob = bullet.ShooterJob;
//         if(!JobSetting.TryGetPlayerLayerSettings(shooterJob,out var playerLayerSettings))
//         {
//             return;
//         }

//         if (playerLayerSettings.IsAttackableJob(nEnemy.EnemyJob))
//         {
//             PlayValidHitClientRpc(fxPosition);
//         }
//         else
//         {
//             PlayInvalidHitClientRpc(fxPosition);
//         }
//     }

//     private bool TryGetShooterJobServer(IResultCollector shooter, out PlayerJob playerJob)
//     {
//         playerJob = PlayerJob.Nothing;
//         if (shooter == null) return false;
//         if (!NetworkManager.ConnectedClients.TryGetValue(shooter.ClientId, out var client))
//             return false;

//         if (client.PlayerObject == null) return false;

//         SyncroPropaty propaty = client.PlayerObject.GetComponentInChildren<SyncroPropaty>();
//         if (propaty == null) return false;
//         playerJob = propaty.Job;
//         return true;
//     }

//     private ulong[] CollectVisibleClientIdsServer()
//     {
//         List<ulong> ids = new();

//         foreach (var pair in NetworkManager.Singleton.ConnectedClients)
//         {
//             NetworkObject playerObject = pair.Value.PlayerObject;
//             if (playerObject == null) continue;

//             SyncroPropaty propaty = playerObject.GetComponentInChildren<SyncroPropaty>();
//             if (propaty == null) continue;

//             if (JobSetting.TryGetPlayerLayerSettings(propaty.Job, out var playerLayerSettings) && playerLayerSettings.IsVisibleLayer(gameObject.layer))
//             {
//                 ids.Add(pair.Key);
//             }
//         }

//         return ids.ToArray();
//     }

//     [Rpc(SendTo.ClientsAndHost)]
//     private void PlayValidHitClientRpc(Vector3 position)
//     {
//         gameEffectEvent.Invoke(new GameEffect(
//             validHitSfxAll,
//             validHitFxPrefabAll,
//             position,
//             volume:validHitVolumeAll
//             ));
//     }

//     [Rpc(SendTo.ClientsAndHost)]
//     private void PlayInvalidHitClientRpc(Vector3 position)
//     {
//         gameEffectEvent.Invoke(new GameEffect(
//             invalidHitSfxAll,
//             invalidHitFxPrefabAll,
//             position,
//             volume: invalidHitVolumeAll
//             ));
//     }
// }



//水野作製
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NEnemyImpactFxReceiver : NetworkBehaviour
{
    [SerializeField] private NEnemy nEnemy;

    [Header("Valid Hit FX / SFX")]
    [SerializeField] private GameObject validHitFxPrefabAll;
    [SerializeField] private AudioClip validHitSfxAll;
    [SerializeField, Range(0f, 1f)] private float validHitVolumeAll = 1f;

    [Header("Invalid Hit FX / SFX")]
    [SerializeField] private GameObject invalidHitFxPrefabAll;
    [SerializeField] private AudioClip invalidHitSfxAll;
    [SerializeField, Range(0f, 1f)] private float invalidHitVolumeAll = 1f;

    [Header("Setting")]
    [SerializeField] private JobSettingGenerator JobSetting;

    [Header("Publish Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    // 同じ弾で多重ヒットしないようにする
    private readonly HashSet<BulletBaseController> handledBullets = new();

    // 出現直後の接触を無視するためのフラグ
    private bool canReceiveHit = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(EnableHitAfterOneFixedUpdate());
        }
    }

    private IEnumerator EnableHitAfterOneFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        canReceiveHit = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !canReceiveHit) return;
        TryHandleBulletHitServer(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !canReceiveHit) return;
        TryHandleBulletHitServer(collision.collider);
    }

    private void TryHandleBulletHitServer(Collider other)
    {
        if (other == null) return;

        IDamageSender damageSender =
            other.GetComponent<IDamageSender>() ??
            other.GetComponentInParent<IDamageSender>();

        if (damageSender == null) return;
        if (damageSender is not BulletBaseController bullet) return;
        // 自分と同じJobの弾は無視する
        if (bullet.ShooterJob == nEnemy.EnemyJob) return;

        // 同じ弾で複数回反応しない
        if (!handledBullets.Add(bullet)) return;

        Vector3 fxPosition = other.ClosestPoint(transform.position);
        if (fxPosition == Vector3.zero)
        {
            fxPosition = other.transform.position;
        }

        PlayerJob shooterJob = bullet.ShooterJob;

        if (!JobSetting.TryGetPlayerLayerSettings(shooterJob, out var playerLayerSettings))
        {
            return;
        }

        if (playerLayerSettings.IsAttackableJob(nEnemy.EnemyJob))
        {
            PlayValidHitRpc(fxPosition);
        }
        else
        {
            PlayInvalidHitRpc(fxPosition);
        }
    }
//PlayValid(Cliant消した)HitRpc
    [Rpc(SendTo.ClientsAndHost)]
    private void PlayValidHitRpc(Vector3 position)
    {
        Debug.Log($"Hit FX RPC fired: {name} pos={position}");
        if (gameEffectEvent == null) return;

        gameEffectEvent.Invoke(new GameEffect(
            validHitSfxAll,
            validHitFxPrefabAll,
            position,
            volume: validHitVolumeAll
        ));
    }
//PlayInValid(Cliant消した)HitRpc
    [Rpc(SendTo.ClientsAndHost)]
    private void PlayInvalidHitRpc(Vector3 position)
    {
        Debug.Log($"Hit FX RPC fired: {name} pos={position}");
        if (gameEffectEvent == null) return;

        gameEffectEvent.Invoke(new GameEffect(
            invalidHitSfxAll,
            invalidHitFxPrefabAll,
            position,
            volume: invalidHitVolumeAll
        ));
    }
}
//水野以上