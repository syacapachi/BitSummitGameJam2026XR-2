using Syacapachi.Attribute;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IDamageReciever
{
    [SerializeField] int maxHP = 100;
    [SerializeField]
    NetworkVariable<float> currentHP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [Header("Publish Event")]
    [SerializeField] HPInfoEvent HpInfoRpcEvent;

    public GameObject GameObject => this.gameObject;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => maxHP;

    public override void OnNetworkSpawn()
    {
        currentHP.OnValueChanged += OnServerHPChanged;
        if (IsServer)
        {
            currentHP.Value = maxHP;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHP.OnValueChanged -= OnServerHPChanged;
    }

    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (!IsServer) return;

        if (currentHP.Value <= 0)
        {
            return;
        }

        currentHP.Value = Mathf.Max(currentHP.Value - damage, 0);

        if (currentHP.Value <= 0)
        {
            OnPlayerDead();
        }
    }
    private void OnServerHPChanged(float oldHP, float newHP)
    {
        // ★追加: UIにHP変化を通知
        HpInfoRpcEvent.Invoke(new HPInfo(newHP, maxHP));

        if (newHP <= 0)
        {
            Debug.Log($"Player {OwnerClientId} has died.",gameObject);
        }
    }
    private void OnPlayerDead()
    {
        Debug.Log($"[" +
            $"[{nameof(PlayerHealth)}] OnPlayerDead called for Player {OwnerClientId}",gameObject);
    }
}
public readonly struct HPInfo
{
    public readonly float CurrentHP;
    public readonly float MaxHP;
    public HPInfo(float currentHp, float maxHp)
    {
        this.CurrentHP = currentHp;
        this.MaxHP = maxHp;
    }
    public readonly override string ToString()
    {
        return $"current:{CurrentHP},Max{MaxHP}";
    }
}