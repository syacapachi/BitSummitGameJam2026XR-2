using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody),typeof(TrailRenderer))]
public class LocalBullet : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] TrailRenderer trailRenderer;
    [SerializeField] float lifeTime = 5f;
    float bulletSpped;
    private void OnDisable()
    {
        trailRenderer.Clear();
    }
    private void Start()
    {
        rb.isKinematic = false;
        rb.linearVelocity = transform.forward * bulletSpped;
        ManagerLocator.Instance.LocalObjectPool.Release(gameObject, lifeTime);
    }

    public void BulletInit(BulletSetting setting)
    {
        bulletSpped = setting.Speed;
    }
#if UNITY_EDITOR
    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
    }
#endif
}
