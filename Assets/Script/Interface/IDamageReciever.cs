using UnityEngine;
using Unity.Netcode;
public interface IDamageReciever
{
    public GameObject GameObject { get; }
    public float CurrentHealth { get; }
    public float MaxHealth { get; }
    public void TakeDamage(float damage);
}
