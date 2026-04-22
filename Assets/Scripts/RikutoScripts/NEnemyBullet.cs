using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    public PlayerJob enemyJob; // Human / Ghost

    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        Debug.Log($"[NEnemyBullet] OnHitServer called. hit: {reciever.GameObject.name}");

        // ─── 敵自身への誤ヒットを除外 ───────────────────────────────
        // 衝突相手が NEnemy を持っている（= 敵オブジェクト）なら無視
        if (other.GetComponentInParent<NEnemy>() != null)
        {
            Debug.Log($"[NEnemyBullet] Hit enemy object: {other.name}, skipping.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
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
                        TryDamagePlayer(reciever, gameManager);
                    }
                    return;
                }

            case GameMode.Survival:
            default:
                {
                    TryDamagePlayer(reciever, gameManager);
                    return;
                }
        }
    }

    /// <summary>
    /// PlayerCollider かどうか確認しジョブフィルタを適用してダメージを与える
    /// </summary>
    private void TryDamagePlayer(IDamageReciever reciever, NGameManager gameManager)
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

        // NetworkPlayerRoot を取得
        var playerRoot = playerCollider.GetComponentInParent<NetworkPlayerRoot>();
        if (playerRoot == null)
        {
            Debug.LogWarning("[NEnemyBullet] NetworkPlayerRoot not found on player.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        // SyncroPropaty（ジョブ情報）を取得
        var prop = playerRoot.propaty;
        if (prop == null)
        {
            Debug.LogWarning("[NEnemyBullet] SyncroPropaty not found on player.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        var playerJob = prop.Job;

        // ジョブフィルタ: enemyJob と playerJob のビットが重なっている場合のみターゲット
        // 例) enemyJob=Ghost, playerJob=Ghost → (Ghost & Ghost) != 0 → ターゲット
        //     enemyJob=Ghost, playerJob=Nothing → (Ghost & Nothing) == 0 → スキップ
        bool canTarget = (enemyJob & playerJob) != 0 || playerJob == PlayerJob.Nothing;
        if (!canTarget)
        {
            Debug.Log($"[NEnemyBullet] Player job {playerJob} not targeted by {enemyJob}, skipping.");
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        // ダメージ適用
        reciever.TakeDamage(this, Damage);
        Debug.Log($"[NEnemyBullet] Player {playerRoot.OwnerClientId} took {Damage} damage! (job: {playerJob})");

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    /// <summary>
    /// Protect モード: ProtectArea へのダメージ処理
    /// </summary>
    private void ApplyProtectDamage(NGameManager gameManager)
    {
        gameManager.BulletHitProtectArea((int)-Damage);
        Debug.Log($"[NEnemyBullet] ProtectArea took {Damage} damage.");

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
