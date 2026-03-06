using UnityEngine;

public class NBulletImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip hitEnemyClip;
    [SerializeField] private AudioClip hitWallClip;

    [SerializeField, Range(0f, 1f)] private float hitEnemyVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float hitWallVolume = 0.8f;

    private bool alreadyPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 point = other.ClosestPoint(transform.position);
        HandleImpact(other.transform, point);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        HandleImpact(collision.transform, point);
    }

    private void HandleImpact(Transform target, Vector3 point)
    {
        if (alreadyPlayed) return;
        alreadyPlayed = true;

        bool hitEnemy =
            target.GetComponent<NEnemy>() != null ||
            target.GetComponentInParent<NEnemy>() != null;

        if (hitEnemy)
        {
            GameAudioManager.Instance?.PlayWorld(hitEnemyClip, point, hitEnemyVolume);
        }
        else
        {
            GameAudioManager.Instance?.PlayWorld(hitWallClip, point, hitWallVolume);
        }
    }
}