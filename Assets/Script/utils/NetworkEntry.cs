using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct NetworkEntry : INetworkSerializable,IEquatable<NetworkEntry>
{
    public FixedString64Bytes key ;
    public NetworkBehaviourReference reference ;

    public FixedString64Bytes Key => key;

    public NetworkBehaviourReference Reference => reference;

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
}
