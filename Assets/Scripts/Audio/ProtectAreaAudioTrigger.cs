using UnityEngine;

public class ProtectAreaAudioTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip reachClipAll;
    [SerializeField, Range(0f, 1f)] private float reachVolumeAll = 1f;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    private void OnTriggerEnter(Collider other)
    {
        var enemyAll = other.GetComponent<NEnemy>() ?? other.GetComponentInParent<NEnemy>();
        if (enemyAll == null) return;

        Vector3 pointAll = other.ClosestPoint(transform.position);

        var despawnAudioAll = other.GetComponent<NEnemyDespawnAudio>() ??
                              other.GetComponentInParent<NEnemyDespawnAudio>();

        if (despawnAudioAll != null)
        {
            despawnAudioAll.MarkReachedGoalServer();
        }

        gameEffectEvent.Invoke(new GameEffect(reachClipAll, null, pointAll, reachVolumeAll));
    }
}