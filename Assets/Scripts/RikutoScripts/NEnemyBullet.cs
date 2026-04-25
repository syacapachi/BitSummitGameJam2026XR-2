using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        Debug.Log($"[{nameof(NEnemyBullet)}] OnHitServer called. hit: {reciever.GameObject.name}");

        // ─── 敵自身への誤ヒットを除外 ───────────────────────────────
        // 衝突相手が IEnemy を持っている（= 敵オブジェクト）なら無視
        if (other.GetComponentInParent<IEnemy>() != null)
        {
            Debug.Log($"[{nameof(NEnemyBullet)}] Hit enemy object: {other.name}, skipping.");
            return;
        }

        var gameManager = ManagerLocator.Instance.AllGameManager;

        switch (gameManager.CurrentGameMode)
        {
            case GameMode.Protect:
                {
                    var protectArea = gameManager.ProtectArea;
                    if (protectArea != null && reciever.GameObject == protectArea)
                    {
                        ApplyProtectDamage(gameManager);
                    }
                    else
                    {
                        // Protect モードでもプレイヤーに当たった場合はダメージ
                        TryDamagePlayer(reciever);
                    }
                    return;
                }

            case GameMode.Survival:
            default:
                {
                    TryDamagePlayer(reciever);
                    return;
                }
        }
    }

    /// <summary>
    /// PlayerCollider かどうか確認しジョブフィルタを適用してダメージを与える
    /// </summary>
    private void TryDamagePlayer(IDamageReciever reciever)
    {
        // PlayerCollider かどうかを確認
        var playerCollider = reciever as PlayerCollider;
        if (playerCollider == null)
        {
            Debug.Log($"[NEnemyBullet] Hit non-player object: {reciever.GameObject.name}, skipping damage.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        // SyncroPropaty（ジョブ情報）を取得
        var playerProp = playerCollider.PlayerProp;
        
        if (playerProp == null)
        {
            Debug.LogWarning("[NEnemyBullet] SyncroPropaty not found on player.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        var playerJob = playerProp.Job;

        // ジョブフィルタ: enemyJob と playerJob のビットが重なっている場合のみターゲット
        // 例) enemyJob=Ghost, playerJob=Ghost → (Ghost & Ghost) != 0 → ターゲット
        //     enemyJob=Ghost, playerJob=Nothing → (Ghost & Nothing) == 0 → スキップ
        bool canTarget = (ShooterJob & playerJob) != 0 || playerJob == PlayerJob.Nothing;
        if (!canTarget)
        {
            Debug.Log($"[NEnemyBullet] Player job {playerJob} not targeted by {ShooterJob}, skipping.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        // ダメージ適用
        reciever.TakeDamage(this, Damage);
        var gameManager = ManagerLocator.Instance.AllGameManager;
        ApplyProtectDamage(gameManager);
        Debug.Log($"[NEnemyBullet] Player {playerProp.OwnerClientId} took {Damage} damage! (job: {playerJob})");

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    /// <summary>
    /// Protect モード: ProtectArea へのダメージ処理,UIの都合でPlayerもこっちにする。
    /// </summary>
    private void ApplyProtectDamage(NetworkGameManager gameManager)
    {
        gameManager.BulletHitProtectArea((int)-Damage);
        Debug.Log($"[NEnemyBullet] ProtectArea took {Damage} damage.");
    }
}
