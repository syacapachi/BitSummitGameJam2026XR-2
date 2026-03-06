using UnityEngine;

public class ProtectAreaAudioTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip reachClip;
    [SerializeField, Range(0f, 1f)] private float reachVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<NEnemy>() ?? other.GetComponentInParent<NEnemy>();
        if (enemy == null) return;

        Vector3 point = other.ClosestPoint(transform.position);

        var despawnAudio = other.GetComponent<NEnemyDespawnAudio>() ??
                           other.GetComponentInParent<NEnemyDespawnAudio>();

        if (despawnAudio != null)
        {
            despawnAudio.MarkReachedGoal();
        }

        GameAudioManager.Instance?.PlayWorld(reachClip, point, reachVolume);
    }
}