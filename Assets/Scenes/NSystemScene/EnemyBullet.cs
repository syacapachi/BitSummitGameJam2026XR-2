using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : NetworkBehaviour
{
    public EnemySO enemySO;
    float speed;

    private void Start()
    {
        speed = enemySO.BulletSpeed;
    }

    void Update()
    {
        if (!IsServer) return;

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject == ManagerLocator.Instance.GameManager.protectArea)
        {
            Debug.Log("Bullet hit ProtectArea");

            ManagerLocator.Instance.GameManager.AddScore(-100);

            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}