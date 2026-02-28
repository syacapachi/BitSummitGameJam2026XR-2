using UnityEngine;
using Unity.Netcode;

public class EnemyMove : NetworkBehaviour
{
    public float moveSpeed = 3f;
    Transform target;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        target = ManagerLocator.Instance.GameManager.protectArea.transform;
    }

    void Update()
    {
        if (!IsServer || target == null) return;

        MoveToTarget();
    }

    void MoveToTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    // Åö ProtectAreaÇ…ì¸Ç¡ÇΩÇÁ
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject == ManagerLocator.Instance.GameManager.protectArea)
        {
            Debug.Log("Enemy reached ProtectArea");

            // ÉXÉRÉAå∏è≠
            ManagerLocator.Instance.GameManager.AddScore(-100);
            ManagerLocator.Instance.GameManager.EnemyKilled();
            ManagerLocator.Instance.GameManager.Enemycome();

            // ìGçÌèú
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}