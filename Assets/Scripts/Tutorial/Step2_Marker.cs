using System;
using System.Collections.Generic;

public class Step2_Marker : TutorialBase
{
    readonly int playerCount;

    readonly HashSet<ulong> markedPlayers = new();

    private readonly IReadOnlyList<EnemySO> enemies;

    public Step2_Marker(
        int playerCount,
        TutorialSpawner spawner,
        Action onComplete,
        IReadOnlyList<EnemySO> enemies)
        : base(spawner, onComplete)
    {
        this.playerCount = playerCount;
        this.enemies = enemies;
    }

    public sealed override void OnStart()
    {
        markedPlayers.Clear();

        // 敵を生成
        spawner.SpawnTargetsForEachPlayer(playerCount, enemies);

        // 攻撃可能にする
        spawner.ApplyAttackableAfterSpawn(true);
    }

    public sealed override void OnEnd()
    {
        // 必要なら後始末
    }

    public sealed override void OnMarkerPlaced(ulong playerId)
    {
        markedPlayers.Add(playerId);

        UnityEngine.Debug.Log(
            $"Marker Player Count : {markedPlayers.Count}/{playerCount}");

        if (markedPlayers.Count >= playerCount)
        {
            UnityEngine.Debug.Log("Step2 Marker Complete!");

            onComplete?.Invoke();
        }
    }
}