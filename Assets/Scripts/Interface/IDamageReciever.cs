using UnityEngine;
public interface IDamageReciever
{
    public GameObject GameObject { get; }
    public float CurrentHealth { get; }
    public float MaxHealth { get; }
    public void TakeDamage(IDamageSender sender,float damage);
}
