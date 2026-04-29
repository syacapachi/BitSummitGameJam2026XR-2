using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] private AudioClip phaseChangeClipAll;
    [SerializeField] private AudioClip gameClearClipAll;

    [SerializeField, Range(0f, 1f)] private float phaseVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float clearVolumeAll = 1f;

    [Header("Subscribe Event")]
    [SerializeField] IntEvent OnPhaseChangeEvent;
    [SerializeField] PlayerResultDataArrayEvent OnGameResultRpc;

    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    private void OnEnable()
    {
        OnPhaseChangeEvent.Register(OnPhaseChanged);
        OnGameResultRpc.Register(OnGameFinished);
    }

    private void OnDisable()
    {
        OnPhaseChangeEvent.Unregister(OnPhaseChanged);
        OnGameResultRpc.Unregister(OnGameFinished);
    }

    private void OnPhaseChanged(int newValue)
    {
        if (newValue >= 0)
        {
            gameEffectEvent.Invoke(GameEffect.CreateAudioEffect(phaseChangeClipAll, transform.position, phaseVolumeAll));
        }
    }

    private void OnGameFinished(PlayerResultData[] data)
    {
        gameEffectEvent.Invoke(GameEffect.CreateAudioEffect(gameClearClipAll, transform.position, clearVolumeAll));
    }
}