using Syacapachi.Attribute;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
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
    //これ使えば、動的に敵の状態を変えられる。
    private readonly NetworkVariable<int> enemyId = new(-1);

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
    private EnemySO rpcEnemySO;
    public GameObject GameObject => this.gameObject;
    NetworkObject IEnemy.NetworkObject => this.NetworkObject;
    public EnemyWeaponSettingsSO EnemyWeaponRpc => rpcEnemySO.EnemyWeapon;  
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => rpcEnemySO.Hp;
    /// <summary>
    /// セットはEditor上のみ
    /// </summary>
    public PlayerJob EnemyJob 
    {
        get => enemyJob;
#if UNITY_EDITOR
        set => enemyJob = value;
#endif
    }
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
    public bool CanAttackRpc => rpcEnemySO.CanAttack;

    [SerializeField] Renderer[] renderers;

    private int originalLayerRpc;

    private int currentPointIndexServerOnly;

    public int CurrentPointIndexServerOnly => currentPointIndexServerOnly;


    //[SerializeField]
    //    private PlayerJob[] jobCycle = new PlayerJob[]
    //{
    //    PlayerJob.Demon,
    //    PlayerJob.Ghost,
    //    PlayerJob.Tutorial
    //};

    [SerializeField] EnemyDataBase enemyDataBase;
    public bool IsAttackableJob(PlayerJob playerJob)
    {
        if(enemyJobSetting == null)
        {
            Debug.LogError($"enemyJobSetting is null! {gameObject.name}",gameObject);
            return false;
        }
        if(enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)){
            return setting.IsAttackableJob(playerJob);
        }
        Debug.LogError($"LayerMask setting not found for job: {playerJob}",gameObject);
        return false;
    }
    [OnInspectorButton(ShowOnlyInPlayMode = true)]
    public void InjectSetting(int id,int spawnPointIndex)
    {
        enemyId.Value = id;
        currentPointIndexServerOnly = spawnPointIndex;
    }

    public bool TryGetEnemySO(int id,out EnemySO enemySO)
    {
        enemySO = null;
        if (enemyDataBase == null)
        {
            Debug.LogError($"[{nameof(TryGetEnemySO)}] enemyDataBase is NULL! {gameObject.name}");
            return false;
        }
        //内部で大きさを測ってるのでnullかEnemySOがかえる
        enemySO = enemyDataBase.GetEnemyDataFromId(id);

        return enemySO != null;
    }

    public override void OnNetworkSpawn()
    {
        isInitialize = false;
        isDieServerOnly = false;
        currentHP.OnValueChanged += OnHPChanged;
        //NetworkVariableを使えば、Spawn()前に設定した値も初期化時に同期される。
        if (TryGetEnemySO(enemyId.Value, out rpcEnemySO))
        {
            if (IsServer)
            {
                currentHP.Value = rpcEnemySO.Hp;
            }
            UpdateHPUI(currentHP.Value);
        }
        else
        {
            StartCoroutine(WaitForEnemySO());
        }
        ApplySettting();
        canTakeDamage = true;
        StartCoroutine(SetupPlayerCoroutine());
    }
    IEnumerator WaitForEnemySO()
    {   
        yield return new WaitUntil(() => TryGetEnemySO(enemyId.Value, out rpcEnemySO));
        if (IsServer)
        {
            currentHP.Value = rpcEnemySO.Hp;
        }
        UpdateHPUI(currentHP.Value);
    }
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
                childs.gameObject.layer = setting.CollidersLayer;                
            }
            originalLayerRpc = setting.CollidersLayer;
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
            //プレーヤーをずっと見てくる
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
        if (!IsServer) return;
        //無敵解除
        canTakeDamage = true;
        if (!rpcEnemySO.CanMove) return;
        var CheckPointManager = ManagerLocator.Instance.CheckPointManager;
        if (CheckPointManager == null) return;
        //次へ移動
        int currentPoint = currentPointIndexServerOnly;
        int searchPoint = currentPoint + 1;
        if (searchPoint >= CheckPointManager.SpawnPoints.Length) searchPoint = 0;
        while (CheckPointManager.IsUsingPoint(searchPoint) && searchPoint != currentPoint)
        {
            searchPoint++;
            if (searchPoint >= CheckPointManager.SpawnPoints.Length) searchPoint = 0;
        }
        CheckPointManager.TrySetUsePoint(currentPointIndexServerOnly, false);

        currentPointIndexServerOnly = searchPoint;


        moveCorutine = StartCoroutine(MoveToNextPos(
            CheckPointManager.SpawnPoints[currentPointIndexServerOnly].transform.position
        ));
        CheckPointManager.TrySetUsePoint(currentPointIndexServerOnly, true);
    }
    //水野以上    
    IEnumerator MoveToNextPos(Vector3 targetPos)
    {
        //位置情報の更新はFixedupdateにする。
        var nextFixed = new WaitForFixedUpdate();
        yield return nextFixed;
        while(Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.fixedDeltaTime * rpcEnemySO.MoveSpeedValue);
            yield return nextFixed;
        }
    }
    
    void DieOnServer(IResultCollector collector)
    {
        if (collector != null && collector is PlayerStats stats)
        {
            stats.AddKill(rpcEnemySO, rpcEnemySO.ScoreValue);
        }
        else
        {
            Debug.LogError("collector is null!",gameObject);
        }

        enemyKilled.Invoke(new EnemyKilled() { KilledEnemy = this, positon = transform.position });
        if (enemyJob == PlayerJob.Tutorial)
        {
            DieFromAnimationServerEvent();
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
    public void DieFromAnimationServerEvent()
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
            hpImage.fillAmount = hp / rpcEnemySO.Hp;

        if (hpText != null)
            hpText.text = $"{hp} / {rpcEnemySO.Hp}";
    }
    public void SetAttackabe(bool value)
    {
        canTakeDamage = value;
    }

    //[OnInspectorButton]
    //public void NextJob()
    //{
    //    if (!IsServer) return;

    //    if (jobCycle == null || jobCycle.Length == 0)
    //    {
    //        Debug.LogError("[Enemy] JobCycle is empty");
    //        return;
    //    }

    //    int index = Array.IndexOf(jobCycle, enemyJob);

    //    int nextIndex;

    //    if (index == -1)
    //    {
    //        // 見つからない場合は先頭へ
    //        nextIndex = 0;
    //    }
    //    else
    //    {
    //        nextIndex = (index + 1) % jobCycle.Length;
    //    }

    //    ChangeJobServer(jobCycle[nextIndex]);
    //}
    //// =========================
    //// 🌐 サーバー専用：Job変更
    //// =========================
    //public void ChangeJobServer(PlayerJob newJob)
    //{
    //    if (!IsServer) return;

    //    ApplyJobInternal(newJob);
    //    // 全クライアントへ同期
    //    ChangeJobRpc(newJob);
    //}
    //// =========================
    //// 🌐 クライアント同期
    //// =========================
    //[Rpc(SendTo.ClientsAndHost)]
    //private void ChangeJobRpc(PlayerJob newJob)
    //{
    //    ApplyJobInternal(newJob);
    //}

    //// =========================
    //// 🧠 内部処理
    //// =========================
    //private void ApplyJobInternal(PlayerJob newJob)
    //{
    //    if (enemyJob == newJob) return;

    //    var oldJob = enemyJob;
    //    enemyJob = newJob;

    //    ApplySettting(); // ← 既存のLayer変更処理

    //    Debug.Log($"[Enemy] Job Changed: {oldJob} → {enemyJob}",gameObject);
    //}

    public void SetVisibleFromAnimationEvent()
    {
        ApplyVisible(true);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetVisibleRpc()
    {
        ApplyVisible(true);
    }

    public void RestoreLayerFromAnimationEvent()
    {
        ApplyVisible(false);
    }

    void ApplyVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].gameObject.layer = visible ? 0 : originalLayerRpc;
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
