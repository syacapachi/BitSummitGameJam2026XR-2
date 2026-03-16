using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : NetworkBehaviour
{
    public EnemySO enemySO;
    float speed;

    private void Start()
    {
        speed = enemySO.BulletSpeed;

        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), 5f); // 5秒後に消える
        }
    }

    void DespawnBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    void Update()
    {
        if (!IsServer) return;

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject == ManagerLocator.Instance.AllGameManager.protectArea)
        {
            Debug.Log("Bullet hit ProtectArea");

            ManagerLocator.Instance.AllGameManager.BulletHitProtectArea(-enemySO.Damage);

            DespawnBullet();
        }
    }
}