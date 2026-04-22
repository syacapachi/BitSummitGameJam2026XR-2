using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IDamageReciever
{
    [SerializeField] int maxHP = 100;
    [SerializeField]
    NetworkVariable<float> currentHP = new NetworkVariable<float>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ★追加: HP変化イベント（UIが購読する）
    public event System.Action<float, float> OnHPChanged;

    public GameObject GameObject => this.gameObject;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => maxHP;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHP.Value = maxHP;
        }
        currentHP.OnValueChanged += OnServerHPChanged;
        Debug.Log($"PlayerHealth spawned. owner:{OwnerClientId}, NetworkId:{NetworkObjectId}");
    }

    public override void OnNetworkDespawn()
    {
        currentHP.OnValueChanged -= OnServerHPChanged;
        Debug.Log($"PlayerHealth despawned. owner:{OwnerClientId}, NetworkId:{NetworkObjectId}");
    }

    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (!IsServer) return;

        if (currentHP.Value <= 0)
        {
            Debug.Log($"[PlayerHealth] Player {OwnerClientId} is already dead.");
            return;
        }

        currentHP.Value = Mathf.Max(currentHP.Value - damage, 0);
        Debug.Log($"Player {OwnerClientId} took {damage} damage. Current HP: {currentHP.Value}");

        if (currentHP.Value <= 0)
        {
            Debug.Log($"[PlayerHealth] Player {OwnerClientId} has died!");
            OnPlayerDead();
        }
    }

    private void OnServerHPChanged(float oldHP, float newHP)
    {
        Debug.Log($"Player {OwnerClientId} HP changed from {oldHP} to {newHP}");

        // ★追加: UIにHP変化を通知
        OnHPChanged?.Invoke(newHP, maxHP);

        if (newHP <= 0)
        {
            Debug.Log($"Player {OwnerClientId} has died.");
        }
    }

    private void OnPlayerDead()
    {
        Debug.Log($"[PlayerHealth] OnPlayerDead called for Player {OwnerClientId}");
    }
}
