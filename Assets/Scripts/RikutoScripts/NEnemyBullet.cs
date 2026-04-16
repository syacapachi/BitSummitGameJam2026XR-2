using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;

public class NEnemyBullet : BulletBaseController
{
    public PlayerJob enemyJob; // Human / Ghost
    protected override void OnHitServer(IDamageReciever reciever, GameObject other)
    {
        var gameManager = ManagerLocator.Instance.AllGameManager;

        switch (gameManager.CurrentGameMode)
        {
            case GameMode.Protect:
                {
                    var protectArea = gameManager.ProtectArea;

                    // ProtectAreaに当たったときだけダメージ
                    if (protectArea != null && reciever.GameObject == protectArea)
                    {
                        ApplyDamage();
                    }

                    return;
                }

            case GameMode.Survival:
            default:
                {
                    // プレイヤー判定
                    var players = ManagerLocator.Instance.AllPlayerManager.AllPlayers;

                    foreach (var player in players)
                    {
                        if (player == null) continue;

                        if (reciever.GameObject != player.gameObject) continue;

                        var prop = player.propaty;
                        if (prop == null) return;

                        var playerJob = prop.Job;

                        // フィルタ
                        bool canTarget = (enemyJob & playerJob) != 0;
                        if (!canTarget) return;

                        ApplyDamage();
                        return;
                    }

                    return;
                }
        }

        void ApplyDamage()
        {
            gameManager.BulletHitProtectArea((int)-Damage);

            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }
    }
}