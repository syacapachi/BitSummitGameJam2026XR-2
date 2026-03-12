using UnityEngine;

public class ProtectAreaAudioTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip reachClipAll;
    [SerializeField, Range(0f, 1f)] private float reachVolumeAll = 1f;

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

        GameAudioManager.Instance?.PlayWorld(reachClipAll, pointAll, reachVolumeAll);
    }
}