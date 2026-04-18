using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro; // ← 追加
using System.Collections;

public class NEnemy : NetworkBehaviour,IDamageReciever,IEnemy
{
    [SerializeField] EnemySO enemySO;
    private readonly NetworkVariable<float> currentHP = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Canvas hpCanvas;
    [SerializeField] private Image hpImage; // Filled Image
    [SerializeField] private TextMeshProUGUI hpText;
    [Header("Publish Event")]
    [SerializeField] EnemyKilledEvent enemyKilled;

    private Transform targetPlayer;
    public GameObject GameObject => this.gameObject;
    NetworkObject IEnemy.NetworkObject => this.NetworkObject;
    public EnemySO EnemySO => enemySO;  
    public int Layer => gameObject.layer;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => enemySO.Hp;
    public PlayerJob enemyJob;
    private ulong lastAttackerId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHP.Value = enemySO.Hp;
        }

        currentHP.OnValueChanged += OnHPChanged;

        UpdateHPUI(currentHP.Value);

        if (!IsClient) return;// クライアントでのみ実行

        if (hpImage != null)
        {
            hpImage.fillAmount = 1f;
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP.Value} / {enemySO.Hp}";
        }

        StartCoroutine(SetupPlayerCoroutine());
    }

    private IEnumerator SetupPlayerCoroutine()
    {
        yield return new WaitUntil(() =>
            ManagerLocator.Instance != null &&
            ManagerLocator.Instance.AllPlayerManager != null &&
            ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer != null &&
            ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.transform != null
        );

        targetPlayer = ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.transform;
    }

    void LateUpdate()
    {
        if (hpCanvas == null) return;

        var player = ManagerLocator.Instance?.AllPlayerManager?.NetworkOwnerPlayer;

        if (player == null) return;

        var target = player.transform;

        hpCanvas.transform.LookAt(target);
        hpCanvas.transform.Rotate(0, 180f, 0);
    }

    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (!IsServer) return;

        Debug.Log("Take damage");
        currentHP.Value -= damage;
        if (hpImage != null)
        {
            hpImage.fillAmount = Mathf.Clamp01((float)currentHP.Value / enemySO.Hp);
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP.Value} / {enemySO.Hp}";
        }
        

        if (currentHP.Value <= 0)
        {
            DieRpc();
        }
    }

    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Server)]
    void DieRpc()
    {
        Debug.Log("Die");
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerId, out var client))
        {
            var root = client.PlayerObject.GetComponent<NetworkPlayerRoot>();
            Debug.Log("Attacker found: " + lastAttackerId);
            if (root != null)
            {
                Debug.Log("Add kill");
                root.stats.AddKill(enemySO.Id, enemySO.ScoreValue);
            }
        }

        enemyKilled.Invoke(new EnemyKilled() {KilledEnemy = this,positon = transform.position});
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    void OnHPChanged(float oldValue, float newValue)
    {
        UpdateHPUI(newValue);
    }

    void UpdateHPUI(float hp)
    {
        if (hpImage != null)
            hpImage.fillAmount = Mathf.Clamp01((float)hp / enemySO.Hp);

        if (hpText != null)
            hpText.text = $"{hp} / {enemySO.Hp}";
    }
}
