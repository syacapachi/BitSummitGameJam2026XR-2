using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : NetworkBehaviour
{
    public EnemySO enemySO;
    float speed;
    public BulletState bulletState;

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
    var root = other.GetComponentInParent<PlayerRoot>();
    if (root == null) return;

    var player = root.propaty;
    if (player == null) return;

    var job = player.Job;

    bool hit = false;

    switch (bulletState)
    {
        case BulletState.Human:
            hit = (job & PlayerPropaty.PlayerJob.Human) != 0;
            break;

        case BulletState.Ghost:
            hit = (job & PlayerPropaty.PlayerJob.Ghost) != 0;
            break;

        case BulletState.Both:
            hit = true;
            break;
    }

    if (hit)
    {
        Debug.Log("Player Hit!");
        ManagerLocator.Instance.AllGameManager.BulletHitProtectArea(-enemySO.Damage);

        // ダメージ処理
        // root.playerHealth.TakeDamage(enemySO.Damage);
    }
    else
    {
        Debug.Log("Hit but no damage (state mismatch)");
    }

    DespawnBullet();
}
}