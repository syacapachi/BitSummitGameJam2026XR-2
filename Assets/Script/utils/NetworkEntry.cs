using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetworkEntry : INetworkSerializable,IEquatable<NetworkEntry>
{
    public FixedString64Bytes key ;
    public NetworkBehaviourReference reference ;

    public readonly FixedString64Bytes Key => key;

    public readonly NetworkBehaviourReference Reference => reference;

    public NetworkEntry(FixedString64Bytes _key,NetworkBehaviourReference _reference)
    {
        this.key = _key;
        this.reference = _reference;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref key);
        serializer.SerializeValue(ref reference);
    }

    public bool Equals(NetworkEntry other)
    {
        return other.Key.Equals(this.key) && other.Reference.Equals(this.Reference);
    }
    public override string ToString()
    {
        return $"{key}:{reference}";
    }
}
