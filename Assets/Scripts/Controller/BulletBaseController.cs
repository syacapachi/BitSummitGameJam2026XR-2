using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class BulletBaseController : NetworkBehaviour, IDamageSender
{
    [SerializeField] float lifeTime = 5f;
    [SerializeField] Rigidbody rb;
    WeaponSettingsSO gunSO;
    private PlayerJob shooterJob = PlayerJob.Both;
    private ulong shooterId;
    protected Coroutine despawnTimer;
    public ulong ShooterId => shooterId;
    public GameObject GameObject => gameObject;

    public PlayerJob ShooterJob => shooterJob;

    public float Damage => gunSO.Damage;

    void Start()
    {
        rb ??= GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer && gunSO != null)
        {
            rb.linearVelocity = transform.forward * gunSO.speed;
            despawnTimer = StartCoroutine(DespawnCorutine(lifeTime));
        }
    }
    public void BulletInit(ulong id,PlayerJob shooterJob,WeaponSettingsSO so)
    {
        shooterId = id;
        this.shooterJob = shooterJob;
        gunSO = so;
    }
    private IEnumerator DespawnCorutine(float time)
    {
        yield return new WaitForSeconds(time);
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
}
