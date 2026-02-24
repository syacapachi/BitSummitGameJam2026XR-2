using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NBullet : NetworkBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;
    public int damage = 1;

    Rigidbody rb;
    Coroutine despawnTimer;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = transform.forward * speed;
            despawnTimer = StartCoroutine(DespawnCorutine(lifeTime));
        }
    }

    private IEnumerator DespawnCorutine(float time)
    {
        for(float timer = 0f; timer < time; timer += Time.deltaTime)
        {
            yield return null;
        }
        GetComponent<NetworkObject>().Despawn(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit");
        // Enemy �ɓ��������ꍇ
        NEnemy enemy = collision.gameObject.GetComponent<NEnemy>();
        if (enemy != null)
        {

            enemy.TakeDamage(damage);
        }
        StopCoroutine(despawnTimer);
        GetComponent<NetworkObject>().Despawn(true); // �e�͏�����
    }
}