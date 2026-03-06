using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class NEnemyLoopAudio : NetworkBehaviour
{
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.35f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // 3D音
    }

    public override void OnNetworkSpawn()
    {
        if (loopClip == null) return;

        audioSource.clip = loopClip;
        audioSource.volume = loopVolume;
        audioSource.Play();
    }

    public override void OnNetworkDespawn()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}