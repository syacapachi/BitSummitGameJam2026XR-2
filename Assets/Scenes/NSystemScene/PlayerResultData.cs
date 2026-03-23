using Unity.Netcode;
using System;

[Serializable]
public struct PlayerResultData : INetworkSerializable
{
    public ulong clientId;
    public string playerName;
    public int score;
    public int shotsFired;
    public int hits;
    public int shield;
    public float damageDealt;
    public int[] killCounts;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        if (playerName == null) playerName = "";
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref shotsFired);
        serializer.SerializeValue(ref hits);
        serializer.SerializeValue(ref shield);
        serializer.SerializeValue(ref damageDealt);
        serializer.SerializeValue(ref killCounts);
    }
}