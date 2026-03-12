using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NGun))]
[RequireComponent(typeof(AudioSource))]
public class NGunAudioObserver : NetworkBehaviour
{
    [SerializeField] private AudioClip shotClipAll;
    [SerializeField] private AudioClip reloadClipAll;

    [SerializeField, Range(0f, 1f)] private float shotVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolumeAll = 1f;

    private NGun gunAll;
    private AudioSource audioSourceAll;

    private void Awake()
    {
        gunAll = GetComponent<NGun>();
        audioSourceAll = GetComponent<AudioSource>();

        audioSourceAll.playOnAwake = false;
        audioSourceAll.loop = false;
        audioSourceAll.spatialBlend = 1f;
    }

    public override void OnNetworkSpawn()
    {
        gunAll.syncedAmmo.OnValueChanged += OnAmmoChanged;
        gunAll.isReloading.OnValueChanged += OnReloadChanged;
    }

    public override void OnNetworkDespawn()
    {
        gunAll.syncedAmmo.OnValueChanged -= OnAmmoChanged;
        gunAll.isReloading.OnValueChanged -= OnReloadChanged;
    }

    private void OnAmmoChanged(int oldValue, int newValue)
    {
        if (oldValue > newValue && shotClipAll != null)
        {
            audioSourceAll.PlayOneShot(shotClipAll, shotVolumeAll);
        }
    }

    private void OnReloadChanged(bool oldValue, bool newValue)
    {
        if (!oldValue && newValue && reloadClipAll != null)
        {
            audioSourceAll.PlayOneShot(reloadClipAll, reloadVolumeAll);
        }
    }
}