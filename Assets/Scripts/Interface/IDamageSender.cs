using UnityEngine;

public interface IDamageSender
{
    public GameObject GameObject { get; }
    public float Damage { get; }
    public IResultCollector ResultCollector { get; }
    public void SendDamage(IDamageReciever reciever,float damage);
}
