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
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] GameObject shieldFxPrefab;
    [SerializeField] float hitFxLife = 2f;
    [Header("Publis Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    private void OnDisable()
    {
        trailRenderer.Clear();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SpawnHitFxClientRpc(Vector3 pos)
    {
        //バグが起きたときのために、Prefabから生成する方法も残しておく
        gameEffectEvent.Invoke(new GameEffect(hitAudio.ToRuntimeData(),hitFx.ToRuntimeData(), pos));
        //gameEffectEvent.Invoke(GameEffect.CreateFxEffect(hitFxPrefab, pos, fxLifeTime: hitFxLife));
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SpawnShieldFxClientRpc(Vector3 pos)
    {
        gameEffectEvent.Invoke(new GameEffect(shieldAudio.ToRuntimeData(),shieldFx.ToRuntimeData(), pos));
        //gameEffectEvent.Invoke(GameEffect.CreateFxEffect(shieldFxPrefab, pos, fxLifeTime: hitFxLife));
    }

    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        if (reciever is IEnemy enemy)
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
                $"AttackableJob={layerMaskSetting.AttackableJob}, " +
                $"IsAttackable={layerMaskSetting.IsAttackableJob(enemy.EnemyJob)}"
            );
            if (!enemy.IsAttackable)
            {
                attackBlockedEvent.Invoke(new AttackBlocked()
                {
                    Collector = ResultCollector,
                    Enemy = enemy
                });
                // [追加] 攻撃が無効な敵に当たった場合のデバッグログ
                Debug.Log($"NotDamage");
                return;
            }

            if (ResultCollector == null || ResultCollector is not PlayerStats stats)
            {
                Debug.Log($"ResultCollector is not {nameof(PlayerStats)}");
                return;
            }
            

            if (layerMaskSetting.IsAttackableJob(enemy.EnemyJob))
            {
                //ダメージが通る
                Debug.Log("Damage");
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
                Debug.Log("Shield");
                stats.AddShield();
                attackBlockedEvent.Invoke(new AttackBlocked()
                {
                    Collector = ResultCollector,
                    Enemy = enemy
                });
                //水野編集
                SpawnShieldFxClientRpc(transform.position);
                //水野以上
            }
        }
        else if(reciever is PlayerCollider player || reciever is PlayerHealth health)
        {
            //自身は無視
            if (ResultCollector.ClientId == NetworkManager.LocalClientId) return;
            // 当たるのは敵かプレイヤーなので、敵でなければプレイヤーに当たったとみなす
            Debug.Log("Shield by Player");
            attackBlockedEvent.Invoke(new AttackBlocked()
            {
                Collector = ResultCollector,
                Enemy = null
            });
            SpawnShieldFxClientRpc(transform.position);
            return;
        }
        else
        {
            Debug.Log($"Unkown type");
            return;
        }

        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}