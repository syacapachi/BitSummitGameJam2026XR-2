using UnityEngine;

public class NBulletImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip hitEnemyClipAll;
    [SerializeField] private AudioClip hitWallClipAll;

    [SerializeField, Range(0f, 1f)] private float hitEnemyVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float hitWallVolumeAll = 0.8f;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    private bool alreadyPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 pointAll = other.ClosestPoint(transform.position);
        HandleImpact(other.transform, pointAll);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 pointAll = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        HandleImpact(collision.transform, pointAll);
    }

    private void HandleImpact(Transform targetAll, Vector3 pointAll)
    {
        if (alreadyPlayed) return;
        alreadyPlayed = true;

        bool hitEnemy =
            targetAll.GetComponent<NEnemy>() != null ||
            targetAll.GetComponentInParent<NEnemy>() != null;

        if (hitEnemy)
        {
            gameEffectEvent.Invoke(GameEffect.CreateAudioEffect(hitEnemyClipAll, pointAll, hitEnemyVolumeAll));
        }
        else
        {
            gameEffectEvent.Invoke(GameEffect.CreateAudioEffect(hitWallClipAll, pointAll, hitWallVolumeAll));
        }
    }
}