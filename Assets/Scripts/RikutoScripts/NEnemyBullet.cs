using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    public PlayerJob enemyJob; // Human / Ghost
    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        var protectArea = ManagerLocator.Instance.AllGameManager.ProtectArea;

        // ① ProtectAreaがある場合
        if (protectArea != null)
        {
            if (reciever.GameObject == protectArea)
            {
                ApplyDamage();
            }
            return;
        }

        // ② プレイヤー判定
        var players = ManagerLocator.Instance.AllPlayerManager.AllPlayers;

        foreach (var player in players)
        {
            if (player == null) continue;

            // ★ 当たった相手かチェック
            if (reciever.GameObject != player.gameObject) continue;

            var prop = player.propaty;
            if (prop == null) return;

            var playerJob = prop.Job;

            // ★ フィルタ
            bool canTarget = (enemyJob & playerJob) != 0;
            if (!canTarget) return;

            ApplyDamage();
            return; // ← 処理終わり（ここ重要）
        }

        void ApplyDamage()
        {
            ManagerLocator.Instance.AllGameManager.BulletHitProtectArea((int)-Damage);

            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }
    }
}