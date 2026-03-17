using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro; // ← 追加
using System.Collections;

public class NEnemy : NetworkBehaviour,IDamageReciever
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

    private Transform targetPlayer;
    public GameObject GameObject => this.gameObject;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => enemySO.HP;
    public BulletState enemyType;
    private ulong lastAttackerId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHP.Value = enemySO.HP;
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
            hpText.text = $"{currentHP.Value} / {enemySO.HP}";
        }

        StartCoroutine(SetupPlayerCoroutine());
    }

    private IEnumerator SetupPlayerCoroutine()
    {
        // OwnerPlayer が null でなくなるまで待機
        yield return new WaitUntil(() => ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer != null);

        var localPlayer = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer;

        // Transform が存在するかチェック（通常は必ずある）
        if (localPlayer != null)
        {
            targetPlayer = localPlayer.transform;
        }
    }

    void LateUpdate()
    {
        if (hpCanvas != null)
        {
            // プレイヤー方向を向く
            hpCanvas.transform.LookAt(targetPlayer);
            hpCanvas.transform.Rotate(0, 180f, 0);  // Imageが正面向きになるように回転
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        Debug.Log("Take damage");
        currentHP.Value -= damage;
        if (hpImage != null)
        {
            hpImage.fillAmount = Mathf.Clamp01((float)currentHP.Value / enemySO.HP);
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP.Value} / {enemySO.HP}";
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
            var root = client.PlayerObject.GetComponent<PlayerRoot>();
            Debug.Log("Attacker found: " + lastAttackerId);
            if (root != null)
            {
                Debug.Log("Add kill");
                root.stats.AddKill(enemySO.Name, enemySO.scoreValue);
            }
        }

        ManagerLocator.Instance.AllGameManager.EnemyKilled(enemySO.scoreValue);
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
            hpImage.fillAmount = Mathf.Clamp01((float)hp / enemySO.HP);

        if (hpText != null)
            hpText.text = $"{hp} / {enemySO.HP}";
    }

    public void SetAttacker(ulong attackerId)
    {
        lastAttackerId = attackerId;
    }
}
