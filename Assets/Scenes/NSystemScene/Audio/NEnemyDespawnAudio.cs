using UnityEngine;
using Unity.Netcode;

public class NEnemyDespawnAudio : NetworkBehaviour
{
    [SerializeField] private AudioClip deathClipAll;
    [SerializeField, Range(0f, 1f)] private float deathVolumeAll = 1f;

    private bool reachedGoal = false;

    public void MarkReachedGoalServer()
    {
        reachedGoal = true;
    }

    public override void OnNetworkSpawn()
    {
        reachedGoal = false;
    }

    public override void OnNetworkDespawn()
    {
        if (reachedGoal) return;

        GameAudioManager.Instance?.PlayWorld(deathClipAll, transform.position, deathVolumeAll);
    }
}