using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class BulletBaseController : NetworkBehaviour, IDamageSender
{
    [SerializeField] WeaponSettingsSO gunSO;
    [SerializeField] float lifeTime = 5f;
    [SerializeField] Rigidbody rb;
    private DamageAvailableTarget target = DamageAvailableTarget.Both;
    private ulong shooterId;
    protected Coroutine despawnTimer;
    public ulong ShooterId => shooterId;
    public GameObject GameObject => gameObject;

    public DamageAvailableTarget Target => target;

    public float Damage => gunSO.Damage;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = transform.forward * gunSO.speed;
            despawnTimer = StartCoroutine(DespawnCorutine(lifeTime));
        }
    }
    public void BulletInit(ulong id,DamageAvailableTarget target)
    {
        shooterId = id;
        this.target = target;
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
        reciever.TakeDamage(damage);
    }
}
