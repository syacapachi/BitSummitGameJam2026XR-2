using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class NEnemyLoopAudio : NetworkBehaviour
{
    [SerializeField] AudioSource m_AudioSource;
    [SerializeField] private AudioClip loopClipAll;
    [SerializeField, Range(0f, 1f)] private float loopVolumeAll = 0.35f;



    private void Awake()
    {
        m_AudioSource ??= GetComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
        m_AudioSource.loop = true;
        m_AudioSource.spatialBlend = 1f;
    }

    public override void OnNetworkSpawn()
    {
        if (loopClipAll == null) return;

        m_AudioSource.clip = loopClipAll;
        m_AudioSource.volume = loopVolumeAll;
        m_AudioSource.Play();
    }

    public override void OnNetworkDespawn()
    {
        if (m_AudioSource.isPlaying)
        {
            m_AudioSource.Stop();
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        m_AudioSource ??= GetComponent<AudioSource>();
    }
#endif
}