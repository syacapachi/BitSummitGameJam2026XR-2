using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class NBullet : BulletBaseController
{
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] GameObject shieldFxPrefab;
    [SerializeField] float hitFxLife = 2f;
    
    [ClientRpc]
    void SpawnHitFxClientRpc(Vector3 pos)
    {
        GameObject fx = ManagerLocator.Instance.LocalObjectPool.Get(hitFxPrefab);
        fx.transform.SetPositionAndRotation(pos, Quaternion.identity);
        ManagerLocator.Instance.LocalObjectPool.Release(fx, hitFxLife);
    }

    [ClientRpc]
    void SpawnShieldFxClientRpc(Vector3 pos, ClientRpcParams rpcParams = default)
    {
        GameObject fx = ManagerLocator.Instance.LocalObjectPool.Get(shieldFxPrefab);
        fx.transform.SetPositionAndRotation(pos, Quaternion.identity);
        ManagerLocator.Instance.LocalObjectPool.Release(fx, hitFxLife);
    }

    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        var enemy = other.GetComponent<IEnemy>();
        var nm = NetworkManager.Singleton;

        if (!nm.ConnectedClients.TryGetValue(ShooterId, out var client))
        {
            Debug.LogWarning($"Shooter not found: {ShooterId}");
            return;
        }

        var root = client.PlayerObject.GetComponent<NetworkPlayerRoot>();

        if (enemy != null)
        {
            if(!ManagerLocator.Instance.JobManager.JobLayerMaskDic.TryGetValue(ShooterJob, out var layerMaskSetting))
            {
                Debug.LogError($"LayerMask setting not found for job: {ShooterJob}");
                return;
            }
            // ★弾のstateと敵のtypeを比較
            //見えない敵なら当たる。
            if (!layerMaskSetting.IsVisibleLayer(enemy.Layer))
            {
                // ダメージが通る
                Debug.Log("Damage");
                root.stats.AddHit();
                root.stats.AddDamage(Damage);
                reciever.TakeDamage(this,Damage);
                SpawnHitFxClientRpc(transform.position);
            }
            else
            {
                // シールド
                Debug.Log("Shield");
                root.stats.AddShield();
                ClientRpcParams rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { ShooterId }
                    }
                };

                SpawnShieldFxClientRpc(transform.position, rpcParams);
            }
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