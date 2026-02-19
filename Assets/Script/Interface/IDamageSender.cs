using UnityEngine;

public interface IDamageSender
{
    public GameObject GameObject { get; }
    public int Damage { get; }
    public void SendDamage(IDamageReciever reciever,int damage);
}
