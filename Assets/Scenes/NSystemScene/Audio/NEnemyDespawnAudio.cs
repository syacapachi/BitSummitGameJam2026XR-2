using UnityEngine;
using Unity.Netcode;

public class NEnemyDespawnAudio : NetworkBehaviour
{
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    private bool reachedGoal = false;

    public void MarkReachedGoal()
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

        GameAudioManager.Instance?.PlayWorld(deathClip, transform.position, deathVolume);
    }
}