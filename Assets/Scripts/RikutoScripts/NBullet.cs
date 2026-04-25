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
        // [変更前] other.GetComponent<IEnemy>() のみ
        // [変更後] 親オブジェクトも検索するように修正
        var enemy = other.GetComponent<IEnemy>()
                    ?? other.GetComponentInParent<IEnemy>();

        if (enemy != null)
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
        else
        {
            // [追加] IEnemy が見つからない場合のデバッグログ
            Debug.LogWarning($"IEnemy not found on {other.name} or its parents");
            return;
        }

        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}

/*
void OnCollisionEnter(Collision collision)
{
    Debug.Log("Hit");
    // Enemy �ɓ��������ꍇ
    NEnemy enemy = collision.gameObject.GetComponent<NEnemy>();
    if (enemy != null)
    {

        enemy.TakeDamage();
    }
    StopCoroutine(despawnTimer);
    GetComponent<NetworkObject>().Despawn(true); // �e�͏�����
}
*/
//水野が追加した。ダメージ判定の無効化スクリプト（元に戻しました。）
// if (!IsServer) return;
// Debug.Log("Hit" + other.name);

// NEnemy enemy = other.GetComponent<NEnemy>() ?? other.GetComponentInParent<NEnemy>();
// if (enemy != null)
// {
//     EnemyFxRule rule = enemy.GetComponent<EnemyFxRule>() ?? enemy.GetComponentInParent<EnemyFxRule>();

//     if (rule != null)
//     {
//         PlayerPropaty.PlayerJob shooterJob = PlayerPropaty.PlayerJob.Nothing;

//         if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var client))
//         {
//             var propaty = client.PlayerObject.GetComponentInChildren<PlayerPropaty>();
//             if (propaty != null)
//             {
//                 shooterJob = propaty.Job;
//             }
//         }

//         if (!rule.IsEffectiveFor(shooterJob))
//         {
//             StopCoroutine(despawnTimer);
//             if (NetworkObject.IsSpawned)
//             {
//                 NetworkObject.Despawn(false);
//             }
//             return;
//         }
//     }
// }
//水野が追加した。ダメージ判定の無効化スクリプト
//下の2行消しました。//元に戻しました。

//動いてたやつ
/*
if (!IsServer) return;
Debug.Log("Hit"+other.name);
// Enemy �ɓ��������ꍇ
if (other.TryGetComponent<IDamageReciever>(out var damageReciver))
{
    Debug.Log("Hit DamageReciever" + other.name);

    damageReciver.TakeDamage(gunSO.damage);
    SpawnHitFxClientRpc(transform.position);
    if (NetworkObject.IsSpawned)
    {
        NetworkObject.Despawn(true); // �e�͏�����
    }
}
*/