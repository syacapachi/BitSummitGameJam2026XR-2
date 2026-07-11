using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

public class NEnemy : NetworkBehaviour, IDamageReciever, IEnemy
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
    [SerializeField] HeartHPUI heartUI;
    [SerializeField] PlayerJob enemyJob;
    [SerializeField] NetworkAnimator networkAnimator;
    [Header("Publish Event")]
    [SerializeField] EnemyKilledEvent enemyKilled;
    [SerializeField] GameEffectEvent dieEffectEvent;
    //水野編集
    [SerializeField] private NEnemyDespawnAudio enemyAudio;
    //水野以上
    private bool isInitialize = false;

    private Transform targetPlayerOwnerOnly;
    private EnemySO rpcEnemySO;
    public GameObject GameObject => this.gameObject;
    NetworkObject IEnemy.NetworkObject => this.NetworkObject;
    public EnemyWeaponSettingsSO EnemyWeaponRpc => rpcEnemySO.EnemyWeapon;
    public float CurrentHealth => currentHP.Value;
    public float MaxHealth => rpcEnemySO.Hp;
    private float invMaxHealth;
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
    /// <summary>
    /// 自身が動ける範囲のリスト
    /// </summary>
    readonly List<CheckPointManager.IndexToTransform> movablePointsServerOnly = new();
    /// <summary>
    /// チェックポイントマネージャーのキャッシュ
    /// </summary>
    CheckPointManager checkPointManager;
    /// <summary>
    /// チェックポイントの何番目か
    /// </summary>
    private int currentPointIndexServerOnly = 0;
    /// <summary>
    /// 自身が動ける範囲のリストの何番目か
    /// </summary>
    private int currentMovablePointIndexServerOnly = 0;
    /// <summary>
    /// チェックポイントの何番目か
    /// </summary>
    public int CurrentPointIndexServerOnly => currentPointIndexServerOnly;

    [SerializeField] EnemyDataBase enemyDataBase;
    public bool IsAttackableJob(PlayerJob playerJob)
    {
        if (enemyJobSetting == null)
        {
            Debug.LogError($"enemyJobSetting is null! {gameObject.name}", gameObject);
            return false;
        }
        if (enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)) {
            return setting.IsAttackableJob(playerJob);
        }
        Debug.LogError($"LayerMask setting not found for job: {playerJob}", gameObject);
        return false;
    }
    [OnInspectorButton(ShowOnlyInPlayMode = true)]
    public void InjectSetting(int id, int spawnPointIndex)
    {
        enemyId.Value = id;
        currentPointIndexServerOnly = spawnPointIndex;
    }

    public bool TryGetEnemySO(int id, out EnemySO enemySO)
    {
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
        TryGetEnemySO(enemyId.Value, out rpcEnemySO);
        ApplySettting();
        canTakeDamage = true;
        StartCoroutine(SetupEnemyCoroutine());
    }
    private void ApplySettting()
    {
        if (enemyJobSetting == null)
        {
            Debug.LogError("enemyJonSetting is null!");
            return;
        }

        if (enemyJobSetting.TryGetPlayerLayerSettings(EnemyJob, out var setting)) {
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
    private IEnumerator SetupEnemyCoroutine()
    {
        ManagerLocator locator = ManagerLocator.Instance;
        //
        while (
            locator == null ||
            locator.AllPlayerManager == null ||
            locator.AllPlayerManager.NetworkOwnerPlayer == null ||
            locator.AllPlayerManager.NetworkOwnerPlayer.transform == null ||
            locator.CheckPointManager == null)
        {
            yield return null;
        }
        checkPointManager = locator.CheckPointManager;
        if (IsServer)
        {
            currentHP.Value = rpcEnemySO.Hp;
            invMaxHealth = 1f / rpcEnemySO.Hp;
            checkPointManager.GetSpawnPointByTag(rpcEnemySO.MovablePointTag, movablePointsServerOnly);
            for(int i = 0;i<movablePointsServerOnly.Count; i++)
            {
                if (currentPointIndexServerOnly == movablePointsServerOnly[i].id)
                {
                    //出現地点が、自信の動ける範囲の何番目であるかを持っておく。
                    currentMovablePointIndexServerOnly = i;
                    break;
                }
            }
        }
        UpdateHPUI(0, currentHP.Value);
        //オーナーを見つける。
        targetPlayerOwnerOnly = locator.AllPlayerManager.NetworkOwnerPlayer.transform;
        //移動できる点を確認
        
        isInitialize = true;
    }

    void LateUpdate()
    {
        if (hpCanvas == null) return;
        if (!isInitialize) return;
        if (rootTransfrom != null)
        {
            //プレーヤーをずっと見てくる
            rootTransfrom.LookAt(targetPlayerOwnerOnly);
        }
        hpCanvas.transform.LookAt(targetPlayerOwnerOnly);
        hpCanvas.transform.Rotate(0, 180f, 0);
    }
    //水野編集
    public void TakeDamage(IDamageSender sender, float damage)
    {
        if (!IsServer) return;
        if (isDieServerOnly) return;
        using var log = LogScope.Create(this);
        if (currentHP.Value > 0)  currentHP.Value -= damage;

        if (currentHP.Value <= 0)
        {
            currentHP.Value = 0;
            if(moveCorutine != null)
            {
                //移動を停止。
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
    /// <summary>
    /// Hitアニメーションが終わると呼ばれる。
    /// </summary>
    public void MovePositionFromAnimationServerEvent()
    {
        if (!IsServer) return;
        //無敵解除
        canTakeDamage = true;
        if (!rpcEnemySO.CanMove) return;
        //次へ移動
        int currentPoint = currentMovablePointIndexServerOnly;
        int searchPoint = currentMovablePointIndexServerOnly + 1;
        //はみ出したなら初期値
        if (searchPoint >= movablePointsServerOnly.Count) searchPoint = 0;
        //動ける範囲で次へ
        while (checkPointManager.IsUsingPoint(movablePointsServerOnly[searchPoint].id) && searchPoint != currentPoint)
        {
            searchPoint++;
            if (searchPoint >= movablePointsServerOnly.Count) searchPoint = 0;
        }
        //セマフォ開放
        checkPointManager.TrySetUsePoint(currentPointIndexServerOnly, false);
        //インデックス更新
        currentPointIndexServerOnly = movablePointsServerOnly[searchPoint].id;
        currentMovablePointIndexServerOnly = searchPoint;
        //移動開始
        moveCorutine = StartCoroutine(MoveToNextPos(
            checkPointManager.SpawnPoints[currentPointIndexServerOnly].transform.position
        ));
        //セマフォ取得
        checkPointManager.TrySetUsePoint(currentPointIndexServerOnly, true);
    }
    //水野以上    
    IEnumerator MoveToNextPos(Vector3 targetPos)
    {
        //位置情報の更新はFixedupdateにする。
        yield return WaitForSecondsCache.FixedUpdate;
        while(Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.fixedDeltaTime * rpcEnemySO.MoveSpeedValue);
            yield return WaitForSecondsCache.FixedUpdate;
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
            LogScope.Error("collector is null!");
        }

        enemyKilled.Invoke(new EnemyKilled(transform.position, this));
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
        UpdateHPUI(oldValue, newValue);
    }
    void UpdateHPUI(float oldHp,float newHp)
    {
        if (hpImage != null)
        {
            StartCoroutine(HPChangedCorutine(oldHp * invMaxHealth, newHp * invMaxHealth, 10f));
        }

        if (hpText != null)
            hpText.text = $"{newHp} / {rpcEnemySO.Hp}";
        if (heartUI != null)
            heartUI.SetHP(Mathf.FloorToInt(newHp / 100));
    }
    IEnumerator HPChangedCorutine(float oldRate, float newRate, float t)
    {
        float current = oldRate;
        while (Mathf.Abs(current - newRate) > 0.01f)
        {
            // Time.deltaTime * tで、毎秒t%ずつ変化する用に変化できる。(秒数ではないので注意)
            current = Mathf.Lerp(current, newRate, Time.deltaTime * t);
            hpImage.fillAmount = current;
            yield return null;
        }
        hpImage.fillAmount = newRate;
    }
    public void SetAttackabe(bool value)
    {
        canTakeDamage = value;
    }

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
