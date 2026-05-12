using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] AudioEffectData phaseChangeAudioData;
    [SerializeField] AudioEffectData gameClearAudioData;

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
            gameEffectEvent.Invoke(new GameEffect(phaseChangeAudioData.ToRuntimeData(), transform.position));
        }
    }

    private void OnGameFinished(PlayerResultData[] data)
    {
        gameEffectEvent.Invoke(new GameEffect(gameClearAudioData.ToRuntimeData(), transform.position));
    }
}