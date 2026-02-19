using UnityEngine;
using Unity.Netcode;
public interface IDamageReciever
{
    public GameObject GameObject { get; }
    public NetworkVariable<int> Health { get; }
    public int MaxHealth { get; }
    public void TakeDamage(int damage);
}
