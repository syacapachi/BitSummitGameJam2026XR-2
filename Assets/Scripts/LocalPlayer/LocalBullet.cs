using UnityEngine;
[RequireComponent(typeof(Rigidbody),typeof(TrailRenderer))]
public class LocalBullet : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] TrailRenderer trailRenderer;
    [SerializeField] float lifeTime = 5f;
    private void OnDisable()
    {
        trailRenderer.Clear();
        rb.isKinematic = true;
    }

    public void BulletInit(BulletSetting setting)
    {
        rb.isKinematic = false;
        rb.linearVelocity = transform.forward * setting.Speed;
        ManagerLocator.Instance.LocalObjectPool.Release(gameObject, lifeTime);
    }
#if UNITY_EDITOR
    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
    }
#endif
}
