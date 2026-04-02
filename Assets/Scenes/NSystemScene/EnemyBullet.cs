using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : NetworkBehaviour
{
    public EnemySO enemySO;
    float speed;
    public PlayerJob bulletTargetJob;

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

        // PlayerRootを取得
        var root = other.GetComponentInParent<NetworkPlayerRoot>();
        if (root == null) return;
        Debug.Log("Player Hit!");
        ManagerLocator.Instance.AllGameManager.BulletHitProtectArea(-enemySO.Damage);

        // ダメージ処理
        // root.playerHealth.TakeDamage(enemySO.Damage);


        DespawnBullet();
    }
}