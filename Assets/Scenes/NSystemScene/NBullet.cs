using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NBullet : NetworkBehaviour
{
    public WeaponSettingsSO gunSO;
    public float lifeTime = 5f;

    Rigidbody rb;
    Coroutine despawnTimer;

    public ulong shooterId;

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
        //水野が追加した。ダメージ判定の無効化スクリプト
        if (!IsServer) return;
        Debug.Log("Hit" + other.name);

        NEnemy enemy = other.GetComponent<NEnemy>() ?? other.GetComponentInParent<NEnemy>();
        if (enemy != null)
        {
            EnemyFxRule rule = enemy.GetComponent<EnemyFxRule>() ?? enemy.GetComponentInParent<EnemyFxRule>();

            if (rule != null)
            {
                PlayerPropaty.PlayerJob shooterJob = PlayerPropaty.PlayerJob.Nothing;

                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var client))
                {
                    var propaty = client.PlayerObject.GetComponentInChildren<PlayerPropaty>();
                    if (propaty != null)
                    {
                        shooterJob = propaty.Job;
                    }
                }

                if (!rule.IsEffectiveFor(shooterJob))
                {
                    StopCoroutine(despawnTimer);
                    if (NetworkObject.IsSpawned)
                    {
                        NetworkObject.Despawn(false);
                    }
                    return;
                }
            }
        }
        //水野が追加した。ダメージ判定の無効化スクリプト
        //下の2行消しました。
        // if (!IsServer) return;
        // Debug.Log("Hit"+other.name);
        // Enemy �ɓ��������ꍇ
        if (other.TryGetComponent<IDamageReciever>(out var damageReciver))
        {
            Debug.Log("Hit DamageReciever" + other.name);
            damageReciver.TakeDamage(gunSO.damage);
            StopCoroutine(despawnTimer);
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(false); // �e�͏�����
            }
        }
    }
    
}