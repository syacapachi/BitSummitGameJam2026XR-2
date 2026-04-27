using Syacapachi.Attribute;
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NEnemy : NetworkBehaviour,IDamageReciever,IEnemy
{
    [SerializeField] Transform rootTransfrom;
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
    [SerializeField] GameEffectEvent dieEffectEvent;
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

    [SerializeField]
        private PlayerJob[] jobCycle = new PlayerJob[]
    {
        PlayerJob.Demon,
        PlayerJob.Ghost,
        PlayerJob.Tutorial
    };
    public bool IsAttackableJob(PlayerJob playerJob)
    {
        if(enemyJobSetting == null)
        {
            Debug.LogError("enemyJobSetting is null!");
            return false;
        }
        if(enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)){
            return setting.IsAttackableJob(playerJob);
        }
        Debug.LogError($"LayerMask setting not found for job: {playerJob}");
        return false;
    }
    public override void OnNetworkSpawn()
    {
        isInitialize = false;
        if (IsServer)
        {
            currentHP.Value = enemySO.Hp;
        }

        currentHP.OnValueChanged += OnHPChanged;

        UpdateHPUI(currentHP.Value);
        ApplySettting();

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
        if(enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)){
            foreach (Transform childs in transform.GetComponentsInChildren<Transform>())
            {
                childs.gameObject.layer = setting.Layer;
            }
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
        if (rootTransfrom != null)
        {
            rootTransfrom.LookAt(targetPlayer);
        }
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
        //プレイヤーに寄ってくる
        //StartCoroutine(MoveToNextPos(targetPlayer.position));
        

        if (currentHP.Value <= 0)
        {
            DieOnServer(sender.ResultCollector);
        }
    }

    IEnumerator MoveToNextPos(Vector3 targetPos)
    {
        //位置情報の更新はFixedupdateにする。
        var nextFixed = new WaitForFixedUpdate();
        yield return nextFixed;
        while(Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * enemySO.MoveSpeedValue);
            yield return nextFixed;
        }
    }
    

    void DieOnServer(IResultCollector collector)
    {
        Debug.Log("Die");
        if (collector != null && collector is PlayerStats stats)
        {
            Debug.Log("Add kill");
            stats.AddKill(enemySO, enemySO.ScoreValue);
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
        isInitialize = true;
    }
    void OnHPChanged(float oldValue, float newValue)
    {
        UpdateHPUI(newValue);
        // 死亡エフェクトの発火
        if (newValue <= 0)
        {
            //dieEffectEvent.Invoke(new GameEffect() {  });
        }
    }

    void UpdateHPUI(float hp)
    {
        if (hpImage != null)
            hpImage.fillAmount = Mathf.Clamp01((float)hp / enemySO.Hp);

        if (hpText != null)
            hpText.text = $"{hp} / {enemySO.Hp}";
    }

    public void SetAttackabe(bool value)
    {
        isAttackable = value;
    }

    [OnInspectorButton("NextJob")]
    // =========================
    public void NextJob()
    {
        if (!IsServer) return;

        if (jobCycle == null || jobCycle.Length == 0)
        {
            Debug.LogError("[Enemy] JobCycle is empty");
            return;
        }

        int index = Array.IndexOf(jobCycle, enemyJob);

        int nextIndex;

        if (index == -1)
        {
            // 見つからない場合は先頭へ
            nextIndex = 0;
        }
        else
        {
            nextIndex = (index + 1) % jobCycle.Length;
        }

        ChangeJobServer(jobCycle[nextIndex]);
    }

    // =========================
    // 🌐 サーバー専用：Job変更
    // =========================
    public void ChangeJobServer(PlayerJob newJob)
    {
        if (!IsServer) return;

        ApplyJobInternal(newJob);

        // 全クライアントへ同期
        ChangeJobRpc(newJob);
    }

    // =========================
    // 🌐 クライアント同期
    // =========================
    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeJobRpc(PlayerJob newJob)
    {
        ApplyJobInternal(newJob);
    }

    // =========================
    // 🧠 内部処理
    // =========================
    private void ApplyJobInternal(PlayerJob newJob)
    {
        if (enemyJob == newJob) return;

        var oldJob = enemyJob;
        enemyJob = newJob;

        ApplySettting(); // ← 既存のLayer変更処理

        Debug.Log($"[Enemy] Job Changed: {oldJob} → {enemyJob}");
    }
}
