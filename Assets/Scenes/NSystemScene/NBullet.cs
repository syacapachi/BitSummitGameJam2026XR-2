using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NBullet : NetworkBehaviour
{
    public WeaponSettingsSO gunSO;
    public float lifeTime = 5f;
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] GameObject shieldFxPrefab;
    [SerializeField] float hitFxLife = 2f;

    Rigidbody rb;
    Coroutine despawnTimer;

    public ulong shooterId;
    public BulletState state;

    public void SetShooter(ulong id)
    {
        shooterId = id;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = transform.forward * gunSO.speed;
            Invoke(nameof(DespawnBullet), lifeTime);
        }
    }

    private IEnumerator DespawnCorutine(float time)
    {
        WaitForSeconds wait01 = new WaitForSeconds(0.1f);
        for(float timer = 0f; timer < time; timer += 0.1f)
        {
            yield return wait01;
        }
        NetworkObject.Despawn(false);
    }

    void DespawnBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
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


    void OnTriggerEnter(Collider other)
    {
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

        if (!IsServer) return;

        Debug.Log("Hit " + other.name);

        if (other.TryGetComponent<IDamageReciever>(out var damageReciver))
        {
            var enemy = other.GetComponent<NEnemy>();
            var nm = NetworkManager.Singleton;

            if (!nm.ConnectedClients.TryGetValue(shooterId, out var client))
            {
                Debug.LogWarning($"Shooter not found: {shooterId}");
                return;
            }

            var root = client.PlayerObject.GetComponent<PlayerRoot>();
            if (root == null) return;


            if (enemy != null)
            {
                // ★弾のstateと敵のtypeを比較
                if (state == BulletState.Both || state == enemy.enemyType)
                {
                    // ダメージが通る
                    Debug.Log("Damage");
                    root.stats.AddHit();
                    root.stats.AddDamage(gunSO.damage);
                    enemy.SetAttacker(shooterId);
                    damageReciver.TakeDamage(gunSO.damage);
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
                            TargetClientIds = new ulong[] { shooterId }
                        }
                    };

                    SpawnShieldFxClientRpc(transform.position);
                }
            }

            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    [ClientRpc]
    void SpawnHitFxClientRpc(Vector3 pos)
    {
        GameObject fx = Instantiate(hitFxPrefab, pos, Quaternion.identity);
        Destroy(fx, hitFxLife);
    }

    [ClientRpc]
    void SpawnShieldFxClientRpc(Vector3 pos, ClientRpcParams rpcParams = default)
    {
        GameObject fx = Instantiate(shieldFxPrefab, pos, Quaternion.identity);
        Destroy(fx, 2f);
    }
}

public enum BulletState
{
    Human,
    Ghost,
    Both
}