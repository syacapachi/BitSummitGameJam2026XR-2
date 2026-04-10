using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] private AudioClip phaseChangeClipAll;
    [SerializeField] private AudioClip gameClearClipAll;

    [SerializeField, Range(0f, 1f)] private float phaseVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float clearVolumeAll = 1f;

    [SerializeField] IntEvent OnPhaseChangeEvent;
    [SerializeField] PlayerResultDataArrayEvent OnGameResultRpc;

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
            ManagerLocator.Instance.GameAudioManager?.PlayUI(phaseChangeClipAll, phaseVolumeAll);
        }
    }

    private void OnGameFinished(PlayerResultData[] data)
    {
        ManagerLocator.Instance.GameAudioManager?.PlayUI(gameClearClipAll, clearVolumeAll);
    }
}