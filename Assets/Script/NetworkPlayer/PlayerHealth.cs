using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour,IDamageReciever
{
    [SerializeField] int maxHP = 100;
    [SerializeField] NetworkVariable<float> currentHP = new NetworkVariable<float>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
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
        Debug.Log($"PlayerHealth spawned on network. owner:{OwnerClientId},NetworkId = {NetworkObjectId}");
    }
    public override void OnNetworkDespawn()
    {
        currentHP.OnValueChanged -= OnServerHPChanged;
        Debug.Log($"PlayerHealth despawned on network. owner:{OwnerClientId},NetworkId = {NetworkObjectId}");
    }
    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (IsServer)
        {
            currentHP.Value = Mathf.Max(currentHP.Value - damage, 0);
            Debug.Log($"Player {OwnerClientId} took {damage} damage. Current HP: {currentHP.Value}");
        }   
    }
    //クライアントの場合は、サーバーからのHPの変更を監視して、UIの更新や死亡処理などを行う
    private void OnServerHPChanged(float oldHP, float newHP)
    {
        Debug.Log($"Player {OwnerClientId} HP changed from {oldHP} to {newHP}");
        if (newHP <= 0)
        {
            // プレイヤーが死亡した場合の処理をここに追加
            Debug.Log($"Player {OwnerClientId} has died.");
        }
    }
}
