using Syacapachi.Attribute;
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class NEnemy : NetworkBehaviour,IDamageReciever,IEnemy
{
    [SerializeField] Transform rootTransfrom;
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
    [SerializeField] NetworkAnimator networkAnimator;   
    [Header("Publish Event")]
    [SerializeField] EnemyKilledEvent enemyKilled;
    [SerializeField] GameEffectEvent dieEffectEvent;
    //水野編集
    [SerializeField] private NEnemyDespawnAudio enemyAudio;
    //水野以上
    private bool isInitialize = false;

    private Transform targetPlayerServerOnly;
    private EnemySO enemySOServertOnly;
    private float maxHpAll = 0;
    public GameObject GameObject => this.gameObject;
    NetworkObject IEnemy.NetworkObject => this.NetworkObject;
    public EnemyWeaponSettingsSO EnemyWeaponServeronly => enemySOServertOnly.EnemyWeapon;  
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => maxHpAll;
    
    public PlayerJob EnemyJob => enemyJob;
    /// <summary>
    /// 無敵かどうか
    /// </summary>
    private bool canTakeDamage = true;
    private bool isDieServerOnly = false;
    Coroutine moveCorutine;
    /// <summary>
    /// 無敵かどうか
    /// </summary>
    public bool CanTakeDamage => canTakeDamage;
    public bool CanAttackServerOnly => enemySOServertOnly.CanAttack;

    [SerializeField] Renderer[] renderers;

    private int originalLayerServerOnly;

    private int spawnPointIndexServerOnly;

    private int currentPointIndexServerOnly;

    public int SpawnPointIndexServerOnly => spawnPointIndexServerOnly;


    [SerializeField]
        private PlayerJob[] jobCycle = new PlayerJob[]
    {
        PlayerJob.Demon,
        PlayerJob.Ghost,
        PlayerJob.Tutorial
    };

    //[SerializeField] EnemyDataBase enemyDataBase;
    public bool IsAttackableJob(PlayerJob playerJob)
    {
        if(enemyJobSetting == null)
        {
            Debug.LogError("enemyJobSetting is null!",gameObject);
            return false;
        }
        if(enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)){
            return setting.IsAttackableJob(playerJob);
        }
        Debug.LogError($"LayerMask setting not found for job: {playerJob}",gameObject);
        return false;
    }
    [OnInspectorButton(showOnlyInPlayMode = true)]
    public void InjectSetting(EnemySO enemySO,int spawnPointIndex)
    {
        this.enemySOServertOnly = enemySO;
        spawnPointIndexServerOnly = spawnPointIndex;
        currentPointIndexServerOnly = spawnPointIndex;
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //public void InitEnemyRpc(int id)
    //{
    //    Debug.Log($"[InitEnemyRpc] Called | IsServer:{IsServer} IsClient:{IsClient} | id:{id}");

    //    if (enemyDataBase == null)
    //    {
    //        Debug.LogError("[InitEnemyRpc] enemyDataBase is NULL!");
    //        return;
    //    }

    //    if (id < 0 || id >= enemyDataBase.Length)
    //    {
    //        Debug.LogError($"[InitEnemyRpc] Invalid ID: {id}");
    //        return;
    //    }

    //    enemySOServertOnly = enemyDataBase.GetEnemyDataFromId(id);

    //    if (enemySOServertOnly == null)
    //    {
    //        Debug.LogError($"[InitEnemyRpc] enemySOServertOnly is NULL after GetEnemyDataFromId | id:{id}");
    //        return;
    //    }

    //    Debug.Log($"[InitEnemyRpc] SUCCESS | Enemy: {enemySOServertOnly.name}");

    //    // 念のためUI更新
    //    UpdateHPUI(currentHP.Value);
    //}


    public override void OnNetworkSpawn()
    {
        isInitialize = false;
        isDieServerOnly = false;

        currentHP.OnValueChanged += OnHPChanged;
        if (IsServer)
        {
            currentHP.Value = enemySOServertOnly.Hp;
            ApplyMaxHpRpc(enemySOServertOnly.Hp);
        }

        ApplySettting();
        canTakeDamage = true;
        StartCoroutine(SetupPlayerCoroutine());
    }
    //必要な情報は、最大HPのみ
    [Rpc(SendTo.Everyone)]
    private void ApplyMaxHpRpc(float maxHp)
    {
        this.maxHpAll = maxHp;
        if (!IsClient) return;// クライアントでのみ実行

        if (hpImage != null)
        {
            hpImage.fillAmount = 1f;
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP.Value} / {maxHpAll}";
        }
    }

    //public override void OnNetworkSpawn()
    //{
    //    isInitialize = false;

    //    currentHP.OnValueChanged += OnHPChanged;

    //    StartCoroutine(WaitForEnemySO());
    //}

    //IEnumerator WaitForEnemySO()
    //{
    //    // enemySO がセットされるまで待つ
    //    yield return new WaitUntil(() => enemySOServertOnly != null);

    //    // ↓ここから安全に使える
    //    if (IsServer)
    //    {
    //        currentHP.Value = enemySOServertOnly.Hp;
    //    }

    //    UpdateHPUI(currentHP.Value);
    //    ApplySettting();

    //    if (!IsClient) yield break;

    //    if (hpImage != null)
    //        hpImage.fillAmount = 1f;

    //    if (hpText != null)
    //        hpText.text = $"{currentHP.Value} / {enemySOServertOnly.Hp}";

    //    StartCoroutine(SetupPlayerCoroutine());
    //}
    private void ApplySettting()
    {
        if(enemyJobSetting == null)
        {
            Debug.LogError("enemyJonSetting is null!");
            return;
        }

        if (enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)){
            foreach (Transform childs in transform.GetComponentsInChildren<Transform>())
            {
                childs.gameObject.layer = setting.Layer;                
            }
            originalLayerServerOnly = setting.Layer;
        }
    }
    public override void OnNetworkDespawn()
    {
        isInitialize = false;
    }
    private IEnumerator SetupPlayerCoroutine()
    {
        ManagerLocator locator = ManagerLocator.Instance;
        yield return new WaitUntil(() =>
            locator != null &&
            locator.AllPlayerManager != null &&
            locator.AllPlayerManager.NetworkOwnerPlayer != null &&
            locator.AllPlayerManager.NetworkOwnerPlayer.transform != null
        );

        targetPlayerServerOnly = locator.AllPlayerManager.NetworkOwnerPlayer.transform;
        isInitialize = true;
    }

    void LateUpdate()
    {
        if (hpCanvas == null) return;
        if (!isInitialize) return;
        if (rootTransfrom != null)
        {
            rootTransfrom.LookAt(targetPlayerServerOnly);
        }
        hpCanvas.transform.LookAt(targetPlayerServerOnly);
        hpCanvas.transform.Rotate(0, 180f, 0);
    }
    //水野編集
    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (!IsServer) return;
        if (isDieServerOnly) return;
        if(currentHP.Value > 0)  currentHP.Value -= damage;

        if (currentHP.Value <= 0)
        {
            currentHP.Value = 0;
            if(moveCorutine != null)
            {
                StopCoroutine(moveCorutine);
            }
            DieOnServer(sender.ResultCollector);
            isDieServerOnly = true;
        }
        else
        {
            networkAnimator.SetTrigger("Hit");
            //アニメーションで見えてる間は無敵
            canTakeDamage = false;
            if (enemyAudio != null)
            {
                enemyAudio.PlayHitVoiceServer();
            }
        }
    }
    public void MovePositionFromAnimationServerEvent()
    {
        if(!IsServer) return ;
        //無敵解除
        canTakeDamage = true;
        if (!enemySOServertOnly.CanMove) return;
        var CheckPointManager = ManagerLocator.Instance.CheckPointManager;
        if (CheckPointManager == null) return;
        //次へ移動
        currentPointIndexServerOnly++;
        if (currentPointIndexServerOnly >= CheckPointManager.SpawnPoints.Length) currentPointIndexServerOnly = 0;

        moveCorutine = StartCoroutine(MoveToNextPos(
            CheckPointManager.SpawnPoints[currentPointIndexServerOnly].transform.position
        ));
    }
    //水野以上    
    IEnumerator MoveToNextPos(Vector3 targetPos)
    {
        //位置情報の更新はFixedupdateにする。
        var nextFixed = new WaitForFixedUpdate();
        yield return nextFixed;
        while(Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.fixedDeltaTime * enemySOServertOnly.MoveSpeedValue);
            yield return nextFixed;
        }
    }
    
    void DieOnServer(IResultCollector collector)
    {
        if (collector != null && collector is PlayerStats stats)
        {
            stats.AddKill(enemySOServertOnly, enemySOServertOnly.ScoreValue);
        }
        else
        {
            Debug.LogError("collector is null!",gameObject);
        }

        enemyKilled.Invoke(new EnemyKilled() { KilledEnemy = this, positon = transform.position });
        if (enemyJob == PlayerJob.Tutorial)
        {
            DieFromAnimationEvent();
        }
        else
        {
            networkAnimator.SetTrigger("Death");
            SetVisibleRpc();
        }
        //アニメーション終了を待つため
        canTakeDamage = false;
    }
    void DieOnspector()
    {
        DieOnServer(null);
        isInitialize = true;
    }
    [OnInspectorButton]
    public void DieFromAnimationEvent()
    {
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
            hpImage.fillAmount = hp / maxHpAll;

        if (hpText != null)
            hpText.text = $"{hp} / {maxHpAll}";
    }
    public void SetAttackabe(bool value)
    {
        canTakeDamage = value;
    }

    [OnInspectorButton]
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

        Debug.Log($"[Enemy] Job Changed: {oldJob} → {enemyJob}",gameObject);
    }

    public void SetVisibleServer()
    {
        ApplyVisible(true);
        //if (!IsServer) return;
        //SetVisibleRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetVisibleRpc()
    {
        ApplyVisible(true);
    }

    public void RestoreLayerServer()
    {
        ApplyVisible(false);
        //if (!IsServer) return;
        //RestoreLayerRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void RestoreLayerRpc()
    {
        ApplyVisible(false);
    }

    void ApplyVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].gameObject.layer = visible ? 0 : originalLayerServerOnly;
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        FindRefernce();
    }
    [OnInspectorButton]
    void FindRefernce()
    {
        renderers = GetComponentsInChildren<Renderer>();
        enemyAudio = GetComponentInChildren<NEnemyDespawnAudio>();
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
    }
#endif
}
