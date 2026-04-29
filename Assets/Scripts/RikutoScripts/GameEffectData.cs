using Unity.Netcode;
using UnityEngine;

public struct GameEffectData : INetworkSerializable
{
    public EffectType type;
    public Vector3 Position;
    public float Volume;
    public float Pitch;
    public float Delay;
    public bool Loop;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Volume);
        serializer.SerializeValue(ref Pitch);
        serializer.SerializeValue(ref Delay);
        serializer.SerializeValue(ref Loop);
    }
}