using UnityEngine;

public class PlayerCollider : MonoBehaviour,IDamageReciever
{
    public GameObject GameObject => this.gameObject;

    public float CurrentHealth => throw new System.NotImplementedException();

    public float MaxHealth => throw new System.NotImplementedException();

    public void TakeDamage(IDamageSender sender, float damage)
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
