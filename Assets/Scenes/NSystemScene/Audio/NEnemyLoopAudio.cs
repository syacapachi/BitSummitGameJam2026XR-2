using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class NEnemyLoopAudio : NetworkBehaviour
{
    [SerializeField] private AudioClip loopClipAll;
    [SerializeField, Range(0f, 1f)] private float loopVolumeAll = 0.35f;

    private AudioSource audioSourceAll;

    private void Awake()
    {
        audioSourceAll = GetComponent<AudioSource>();
        audioSourceAll.playOnAwake = false;
        audioSourceAll.loop = true;
        audioSourceAll.spatialBlend = 1f;
    }

    public override void OnNetworkSpawn()
    {
        if (loopClipAll == null) return;

        audioSourceAll.clip = loopClipAll;
        audioSourceAll.volume = loopVolumeAll;
        audioSourceAll.Play();
    }

    public override void OnNetworkDespawn()
    {
        if (audioSourceAll.isPlaying)
        {
            audioSourceAll.Stop();
        }
    }
}