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
            despawnTimer = StartCoroutine(DespawnCorutine(lifeTime));
        }
    }

    private IEnumerator DespawnCorutine(float time)
    {
        for(float timer = 0f; timer < time; timer += Time.deltaTime)
        {
            yield return null;
        }
        NetworkObject.Despawn(true);
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
        if (!IsServer) return;
        Debug.Log("Hit");
        // Enemy �ɓ��������ꍇ
        NEnemy enemy = other.GetComponent<NEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage();
        }
        StopCoroutine(despawnTimer);
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // �e�͏�����
        }
    }
}