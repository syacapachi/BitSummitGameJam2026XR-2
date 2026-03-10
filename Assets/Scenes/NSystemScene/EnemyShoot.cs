using UnityEngine;
using Unity.Netcode;

public class EnemyShoot : NetworkBehaviour
{
    public EnemySO enemySO;
    public GameObject bulletPrefab;

    Transform target;

    float shootTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        target = ManagerLocator.Instance.GameManager.protectArea.transform;
        shootTimer = enemySO.FirstShootDelay;
    }

    void Update()
    {
        if (!IsServer || target == null) return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = enemySO.shootInterval;
        }
    }

    void Shoot()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.LookRotation(direction)
        );

        bullet.GetComponent<NetworkObject>().Spawn();
    }
}