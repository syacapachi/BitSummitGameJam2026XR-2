// 注意:
// Hit / Shield FX は NBullet 側で制御する。
// このクラスでは、敵が自分の弾に被弾する誤判定などを防ぐための判定のみ行う。
// ここで gameEffectEvent.Invoke を呼ぶと、NBullet 側と二重にFXが出る可能性がある。
//水野作製
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NEnemyImpactFxReceiver : NetworkBehaviour
{
    [SerializeField] private NEnemy nEnemy;

    [Header("Valid Hit FX / SFX")]
    [SerializeField] AudioEffectData validHitAudio;
    [SerializeField] FxEffectData validHitFx;

    [Header("Invalid Hit FX / SFX")]
    [SerializeField] AudioEffectData invalidHitAudio;
    [SerializeField] FxEffectData invalidHitFx;

    [Header("Setting")]
    [SerializeField] private JobSettingGenerator JobSetting;

    [Header("Publish Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    // 同じ弾で多重ヒットしないようにする
    private readonly HashSet<BulletBaseController> handledBullets = new();
    // 同じ弾で多重ヒットしないようにするためのIDリスト
    private readonly List<ulong> clientCasheIds = new();

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
    private ulong[] CollectVisibleClientIdsServer()
    {
        clientCasheIds.Clear();

        foreach (var pair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkObject playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;

            NetworkPlayerPropaty propaty = playerObject.GetComponentInChildren<NetworkPlayerPropaty>();
            if (propaty == null) continue;

            if (JobSetting.TryGetPlayerLayerSettings(propaty.Job, out var playerLayerSettings)
                && playerLayerSettings.IsVisibleLayer(gameObject.layer))
            {
                clientCasheIds.Add(pair.Key);
            }
        }

        return clientCasheIds.ToArray();
}    private void TryHandleBulletHitServer(Collider other)
    {
        if (other == null) return;

        IDamageSender damageSender =
            other.GetComponent<IDamageSender>() ??
            other.GetComponentInParent<IDamageSender>();

        if (damageSender == null) return;
        //プレイヤーの弾ではない場合は無視
        if (damageSender is not NBullet bullet) return;
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

        // この敵が見えるクライアントだけ集める
        ulong[] visibleClientIds = CollectVisibleClientIdsServer();

        if (visibleClientIds.Length == 0)
        {
            return;
        }

        if (playerLayerSettings.IsAttackableJob(nEnemy.EnemyJob))
        {
            PlayValidHitRpc(
                fxPosition,
                RpcTarget.Group(visibleClientIds, RpcTargetUse.Temp)
            );
        }
        else
        {
            PlayInvalidHitRpc(
                fxPosition,
                RpcTarget.Group(visibleClientIds, RpcTargetUse.Temp)
            );
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    //PlayValid(Cliant消した)HitRpc
    private void PlayValidHitRpc(Vector3 position, RpcParams rpcParams = default)
    {
        if (gameEffectEvent == null) return;

        //gameEffectEvent.Invoke(new GameEffect(
        //    validHitAudio.ToRuntimeData(), 
        //    validHitFx.ToRuntimeData(), 
        //    position)
        //);
    }
    //RpcParamsで送るクライアントを指定しているため、SendTo.SpecifiedInParamsを使用

    [Rpc(SendTo.SpecifiedInParams)]
    //PlayInValid(Cliant消した)HitRpc
    private void PlayInvalidHitRpc(Vector3 position, RpcParams rpcParams = default)
    {
        if (gameEffectEvent == null) return;

        //gameEffectEvent.Invoke(new GameEffect(
        //    invalidHitAudio.ToRuntimeData(),
        //    invalidHitFx.ToRuntimeData(),
        //    position)
        //);
    }
}
//水野以上