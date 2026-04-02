using UnityEngine;

public interface IDamageSender
{
    public GameObject GameObject { get; }
    public float Damage { get; }
    public void SendDamage(IDamageReciever reciever,float damage);
}
