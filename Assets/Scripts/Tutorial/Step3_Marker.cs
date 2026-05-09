using System;
using System.Collections.Generic;

public class Step3_Marker : TutorialBase
{
    readonly int playerCount;

    readonly HashSet<ulong> markedPlayers = new();

    public Step3_Marker(
        int playerCount,
        TutorialSpawner spawner,
        Action onComplete)
        : base(spawner, onComplete)
    {
        this.playerCount = playerCount;
    }

    public override void OnStart()
    {
        markedPlayers.Clear();
    }

    public override void OnEnd()
    {
    }

    public override void OnMarkerPlaced(ulong playerId)
    {
        markedPlayers.Add(playerId);

        UnityEngine.Debug.Log(
            $"Marker Player Count : {markedPlayers.Count}/{playerCount}");

        if (markedPlayers.Count >= playerCount)
        {
            UnityEngine.Debug.Log("Step3 Marker Complete!");

            onComplete?.Invoke();
        }
    }
}