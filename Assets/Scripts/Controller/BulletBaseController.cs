using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class BulletBaseController : NetworkBehaviour, IDamageSender
{
    [SerializeField] float lifeTime = 5f;
    [SerializeField] Rigidbody rb;
    [SerializeField] protected JobSettingGenerator setting;
    private BulletSetting bulletSettingServerOnly;
    private PlayerJob shooterJob = PlayerJob.Nothing;
    private IResultCollector shooterId;
    protected Coroutine despawnTimer;
    public GameObject GameObject => gameObject;

    public PlayerJob ShooterJob => shooterJob;

    public float Damage => bulletSettingServerOnly.Damage;

    public IResultCollector ResultCollector => shooterId;

    protected BulletSetting BulletServerOnly => bulletSettingServerOnly;


    void Start()
    {
        rb ??= GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer && bulletSettingServerOnly != null)
        {
            rb ??= GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = transform.forward * bulletSettingServerOnly.Speed;
            despawnTimer = StartCoroutine(DespawnCorutine(lifeTime));
        }
    }
    public void BulletInit(IResultCollector shooter, PlayerJob shooterJob, BulletSetting bulletSetting, bool isApplyLayer = false)
    {
        shooterId = shooter;
        this.shooterJob = shooterJob;
        this.bulletSettingServerOnly = bulletSetting;
        if (isApplyLayer)
        {
            ApplySetting();
        }
    }
    private void ApplySetting()
    {
        if (setting.TryGetPlayerLayerSettings(ShooterJob, out var layersetting))
        {
            foreach (Transform childs in transform.GetComponentsInChildren<Transform>())
            {
                childs.gameObject.layer = layersetting.CollidersLayer;
            }
        }
    }
    private IEnumerator DespawnCorutine(float time)
    {
        yield return WaitForSecondsCache.Get(time);
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        //Debug.Log("Hit " + other.name);
        if (other.TryGetComponent<IDamageReciever>(out var damageReciver))
        {
            OnHitServer(damageReciver, other.gameObject);
        }
        else
        {
            var receiver = other.GetComponentInChildren<IDamageReciever>();
            receiver ??= other.GetComponentInParent<IDamageReciever>();
            if (receiver != null)
            {
                OnHitServer(receiver, other.gameObject);
            }
        }
    }
    /// <summary>
    /// ヒット時の処理を行う。ダメージを与える対象や、与えるダメージ量などはここで決定する。
    /// </summary>
    /// <param name="reciever"></param>
    /// <param name="other"></param>
    protected abstract void OnHitServer(IDamageReciever reciever, GameObject other);
    public void SendDamage(IDamageReciever reciever, float damage)
    {
        reciever.TakeDamage(this,damage);
    }
#if UNITY_EDITOR
    void Reset()
    {
        rb ??= GetComponent<Rigidbody>();
    }
#endif
}
