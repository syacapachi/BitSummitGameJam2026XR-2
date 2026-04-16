/*
using UnityEngine;
using Unity.Netcode;

public class EnemyMove : NetworkBehaviour
{
    public EnemySO enemySO;
    Transform target;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        //target = ManagerLocator.Instance.AllGameManager.protectArea.transform;
        target = null;
    }

    void Update()
    {
        if (!IsServer || target == null) return;

        MoveToTarget();
    }

    void MoveToTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * enemySO.Speed * Time.deltaTime;
    }

    // ★ ProtectAreaに入ったら
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject == ManagerLocator.Instance.AllGameManager.protectArea)
        {
            Debug.Log("Enemy reached ProtectArea");

            ManagerLocator.Instance.AllGameManager.EnemyKilled(-100);
            ManagerLocator.Instance.AllGameManager.Enemycome();

            // 敵削除
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
*/