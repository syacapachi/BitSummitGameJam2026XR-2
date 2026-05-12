using Unity.Netcode;
using System;
using Unity.Collections;
using Syacapachi.Attribute;

[Serializable]
//[GenerateEvent(typeof(GameEventSOBase<>),IsArray = true)]
public class PlayerResultData : INetworkSerializable,IEquatable<PlayerResultData>
{
    public ulong clientId;
    public FixedString128Bytes playerName;
    public int score;
    public int shotsFired;
    public int hits;
    public int shield;
    public float damageDealt;
    public int[] killCounts;

    public bool Equals(PlayerResultData other)
    {
        return this.clientId == other.clientId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        if (playerName.IsEmpty) playerName = "";
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref shotsFired);
        serializer.SerializeValue(ref hits);
        serializer.SerializeValue(ref shield);
        serializer.SerializeValue(ref damageDealt);
        serializer.SerializeValue(ref killCounts);
    }
    public override string ToString()
    {
        return base.ToString();
    }
}