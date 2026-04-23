using Syacapachi.Attribute;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NEnemy : NetworkBehaviour,IDamageReciever,IEnemy
{
    [SerializeField] EnemySO enemySO;
    [SerializeField] JobSettingGenerator enemyJobSetting;
    private readonly NetworkVariable<float> currentHP = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Canvas hpCanvas;
    [SerializeField] private Image hpImage; // Filled Image
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] PlayerJob enemyJob;
    [Header("Publish Event")]
    [SerializeField] EnemyKilledEvent enemyKilled;
    private bool isInitialize = false;

    private Transform targetPlayer;
    public GameObject GameObject => this.gameObject;
    NetworkObject IEnemy.NetworkObject => this.NetworkObject;
    public EnemySO EnemySO => enemySO;  
    public int Layer => gameObject.layer;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => enemySO.Hp;
    
    public PlayerJob EnemyJob => enemyJob;

    [SerializeField] private bool isAttackable = true;

    public bool IsAttackable => isAttackable;

    public override void OnNetworkSpawn()
    {
        isInitialize = false;
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
    private void ApplySettting()
    {
        if(enemyJobSetting == null)
        {
            Debug.LogError("enemyJonSetting is null!");
            return;
        }
        if(enemyJobSetting.JobLayerMaskDic.TryGetValue(EnemyJob, out var setting)){
            gameObject.layer = setting.Layer;
        }
    }
    public override void OnNetworkDespawn()
    {
        isInitialize = false;
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
        isInitialize = true;
    }

    void LateUpdate()
    {
        if (hpCanvas == null) return;
        if (!isInitialize) return;

        hpCanvas.transform.LookAt(targetPlayer);
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
            DieOnServer(sender.ResultCollector);
        }
    }

    

    void DieOnServer(IResultCollector collector)
    {
        Debug.Log("Die");
        if (collector != null && collector is PlayerStats stats)
        {
            Debug.Log("Add kill");
            stats.AddKill(enemySO.Id, enemySO.ScoreValue);
        }

        enemyKilled.Invoke(new EnemyKilled() {KilledEnemy = this,positon = transform.position});
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
    [OnInspectorButton("Die")]
    void DieOnspector()
    {
        DieOnServer(null);
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

    public void setAttackabe(bool value)
    {
        isAttackable = value;
    }
}
