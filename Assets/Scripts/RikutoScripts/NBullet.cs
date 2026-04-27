using Unity.Netcode;
using UnityEngine;


public class NBullet : BulletBaseController
{
    [SerializeField] AttackBlockedEvent attackBlockedEvent;
    [SerializeField] TrailRenderer trailRenderer;
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] GameObject shieldFxPrefab;
    [SerializeField] float hitFxLife = 2f;
    private void OnDisable()
    {
        trailRenderer.Clear();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SpawnHitFxClientRpc(Vector3 pos)
    {
        GameObject fx = ManagerLocator.Instance.LocalObjectPool.Get(hitFxPrefab);
        fx.transform.SetPositionAndRotation(pos, Quaternion.identity);
        ManagerLocator.Instance.LocalObjectPool.Release(fx, hitFxLife);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SpawnShieldFxClientRpc(Vector3 pos)
    {
        GameObject fx = ManagerLocator.Instance.LocalObjectPool.Get(shieldFxPrefab);
        fx.transform.SetPositionAndRotation(pos, Quaternion.identity);
        ManagerLocator.Instance.LocalObjectPool.Release(fx, hitFxLife);
    }

    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        //switch (reciever)
        //{
        //    case IEnemy enemy1:
        //        break;
        //}
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
                    Collector = Shooter,
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
                SpawnHitFxClientRpc(transform.position);
            }
            else
            {
                // シールド
                Debug.Log("Shield");
                stats.AddShield();
                attackBlockedEvent.Invoke(new AttackBlocked()
                {
                    Collector = Shooter,
                    Enemy = enemy
                });
                SpawnShieldFxClientRpc(transform.position);
            }
        }
        else if(reciever is PlayerCollider player || reciever is PlayerHealth health)
        {
            //自身は無視
            if (ResultCollector.ClientId == OwnerClientId) return;
            // 当たるのは敵かプレイヤーなので、敵でなければプレイヤーに当たったとみなす
            Debug.Log("Shield by Player");
            attackBlockedEvent.Invoke(new AttackBlocked()
            {
                Collector = Shooter,
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