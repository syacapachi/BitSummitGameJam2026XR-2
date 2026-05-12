 using UnityEngine;
using Unity.Netcode;
public class NGunAudioObserver : NetworkBehaviour, IShotSound, IReloadSound
{
    [SerializeField] AudioEffectData shotAudioEffect;
    [SerializeField] AudioEffectData reloadAudioEffect;

    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent; 


    public void PlayShotSound()
    {
        gameEffectEvent.Invoke(new GameEffect(shotAudioEffect.ToRuntimeData(), transform.position));
    }
    public void PlayReloadSound()
    {
        gameEffectEvent.Invoke(new GameEffect(reloadAudioEffect.ToRuntimeData(), transform.position));
    }
}