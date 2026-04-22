 using UnityEngine;
using Unity.Netcode;
public class NGunAudioObserver : NetworkBehaviour, IShotSound, IReloadSound
{
    [SerializeField] private AudioClip shotClipAll;
    [SerializeField] private AudioClip reloadClipAll;

    [SerializeField, Range(0f, 1f)] private float shotVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float reloadVolumeAll = 1f;

    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent; 


    public void PlayShotSound()
    {
        gameEffectEvent.Invoke(new GameEffect(shotClipAll, null, transform.position, volume: shotVolumeAll));
    }
    public void PlayReloadSound()
    {
        gameEffectEvent.Invoke(new GameEffect(reloadClipAll, null, transform.position, volume: reloadVolumeAll));
    }
}