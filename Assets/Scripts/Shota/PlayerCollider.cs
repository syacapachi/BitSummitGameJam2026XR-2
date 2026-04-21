using UnityEngine;

public class PlayerCollider : MonoBehaviour, IDamageReciever
{
    [SerializeField] PlayerHealth playerHealth;

    public GameObject GameObject => this.gameObject;

    public float CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : 0f;

    public float MaxHealth => playerHealth != null ? playerHealth.MaxHealth : 0f;

    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(sender, damage);
        }
        else
        {
            Debug.LogError("PlayerHealth ‚ª PlayerCollider ‚Éİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñI");
        }
    }
}
