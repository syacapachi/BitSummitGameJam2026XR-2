using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NGun))]
[RequireComponent(typeof(AudioSource))]
public class NGunAudioObserver : NetworkBehaviour
{
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip reloadClip;

    [SerializeField, Range(0f, 1f)] private float shotVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolume = 1f;

    private NGun gun;
    private AudioSource audioSource;

    private void Awake()
    {
        gun = GetComponent<NGun>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // 3D音
    }

    public override void OnNetworkSpawn()
    {
        gun.syncedAmmo.OnValueChanged += OnAmmoChanged;
        gun.isReloading.OnValueChanged += OnReloadChanged;
    }

    public override void OnNetworkDespawn()
    {
        gun.syncedAmmo.OnValueChanged -= OnAmmoChanged;
        gun.isReloading.OnValueChanged -= OnReloadChanged;
    }

    private void OnAmmoChanged(int oldValue, int newValue)
    {
        // 初期化時の増加は無視、減ったときだけ銃声
        if (oldValue > newValue && shotClip != null)
        {
            audioSource.PlayOneShot(shotClip, shotVolume);
        }
    }

    private void OnReloadChanged(bool oldValue, bool newValue)
    {
        // false -> true のときだけリロード開始音
        if (!oldValue && newValue && reloadClip != null)
        {
            audioSource.PlayOneShot(reloadClip, reloadVolume);
        }
    }
}