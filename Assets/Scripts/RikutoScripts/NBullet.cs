using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;
public class NBullet : BulletBaseController
{
    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] TrailRenderer trailRenderer;
    [Header("hitFx Setting")]
    [SerializeField] AudioEffectData hitAudio;
    [SerializeField] FxEffectData hitFx;
    [Header("shieldFx Setting")]
    [SerializeField] AudioEffectData shieldAudio;
    [SerializeField] FxEffectData shieldFx;
    [Header("Publis Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    private void OnDisable()
    {
        trailRenderer.Clear();
    }
    //ヒットは全員見せる
    [Rpc(SendTo.ClientsAndHost)]
    void SpawnHitFxClientRpc(Vector3 pos)
    {
        gameEffectEvent.Invoke(new GameEffect(hitAudio.ToRuntimeData(),hitFx.ToRuntimeData(), pos));
    }
    //シールドは、打った人だけ見せる。
    [Rpc(SendTo.Owner)]
    void SpawnShieldFxRpc(Vector3 pos)
    {
        gameEffectEvent.Invoke(new GameEffect(shieldAudio.ToRuntimeData(),shieldFx.ToRuntimeData(), pos));
    }

    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        if (reciever is IEnemy enemy)
        {
            CheckEnemy(reciever, enemy);
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
        else if (reciever is PlayerCollider player)
        {
            if (ResultCollector.ClientId == player.OwnerClientId) return;

            //自身は無視
            // 当たるのは敵かプレイヤーなので、敵でなければプレイヤーに当たったとみなす
            Debug.Log("Shield by Player", other);
            SpawnShieldFxRpc(transform.position);
        }
        else if(reciever is PlayerHealth health)
        {
            //自身は無視
            // 当たるのは敵かプレイヤーなので、敵でなければプレイヤーに当たったとみなす
            if (ResultCollector.ClientId == health.OwnerClientId) return;
            Debug.Log("Shield by Player", other);
            SpawnShieldFxRpc(transform.position);
        }
        else
        {
            Debug.Log($"Unkown type", other);
        }
    }
    private void CheckEnemy(IDamageReciever reciever,IEnemy enemy)
    {
        if (!setting.TryGetPlayerLayerSettings(ShooterJob, out var layerMaskSetting))
        {
            Debug.LogError($"LayerMask setting not found for job: {ShooterJob}");
            NetworkObject.Despawn(true);
            return;
        }

        Debug.Log(
            $"ShooterJob={ShooterJob}, " +
            $"EnemyJob={enemy.EnemyJob}, " +
            $"AttackableJob={layerMaskSetting.AttackableJobs}, " +
            $"CanTakeDamage={layerMaskSetting.IsAttackableJob(enemy.EnemyJob)}"
        );
        bool isAttackable = layerMaskSetting.IsAttackableJob(enemy.EnemyJob);
        if (!enemy.CanTakeDamage)
        {
            //シールドがでなかったのと見えない敵を撃ってもstep2クリア可能だったため構造変更
            if (isAttackable)
            {
                Debug.Log($"NotDamage By System");
            }
            else
            {
                attackBlockedEvent.Invoke(new AttackBlocked()
                {
                    Collector = ResultCollector,
                    Enemy = enemy
                });
                // [追加] 攻撃が無効な敵に当たった場合のデバッグログ
                SpawnShieldFxRpc(transform.position);
                Debug.Log($"NotDamage By Job");

            }
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            return;
        }

        if (ResultCollector == null || ResultCollector is not PlayerStats stats)
        {
            Debug.Log($"ResultCollector is not {nameof(PlayerStats)}");
            return;
        }

        if (isAttackable)
        {
            //ダメージが通る
            stats.AddHit();
            stats.AddDamage(Damage);
            reciever.TakeDamage(this, Damage);
            //水野編集
            SpawnHitFxClientRpc(transform.position);
            //水野以上
        }
        else
        {
            // シールド
            stats.AddShield();
            attackBlockedEvent.Invoke(new AttackBlocked()
            {
                Collector = ResultCollector,
                Enemy = enemy
            });
            //水野編集
            SpawnShieldFxRpc(transform.position);
            //水野以上
        }
    }
}