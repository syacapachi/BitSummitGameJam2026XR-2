using Syacapachi.Attribute;
using UnityEngine;

public class PlayerCollider : MonoBehaviour, IDamageReciever
{
    [SerializeField] NetworkPlayerPropaty playerProp;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] Renderer[] m_Renerers;
    public ulong OwnerClientId => playerProp.OwnerClientId;
    public NetworkPlayerPropaty PlayerProp => playerProp;

    public GameObject GameObject => this.gameObject;

    public float CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : 0f;

    public float MaxHealth => playerHealth != null ? playerHealth.MaxHealth : 0f;

    public Renderer[] Renderers => m_Renerers;

    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(sender, damage);
        }
        else
        {
            Debug.LogError("PlayerHealth が PlayerCollider に設定されていません！",gameObject);
        }
    }
#if UNITY_EDITOR
    void Reset()
    {
        Find();
    }
    [OnInspectorButton]
    void Find()
    {
        m_Renerers = transform.parent.GetComponentsInChildren<Renderer>();
    }
#endif
}
