using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class NGunAudioObserver : NetworkBehaviour
{
    [SerializeField] private AudioClip shotClipAll;
    [SerializeField] private AudioClip reloadClipAll;

    [SerializeField, Range(0f, 1f)] private float shotVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolumeAll = 1f;

    private AudioSource audioSourceAll;

    private void Awake()
    {
        audioSourceAll = GetComponent<AudioSource>();

        audioSourceAll.playOnAwake = false;
        audioSourceAll.loop = false;
        audioSourceAll.spatialBlend = 1f;
    }

    public void PlayShotSound()
    {
        audioSourceAll.PlayOneShot(shotClipAll, shotVolumeAll);
    }
    public void PlayReloadSound()
    {
        audioSourceAll.PlayOneShot(reloadClipAll, reloadVolumeAll);
    }
}