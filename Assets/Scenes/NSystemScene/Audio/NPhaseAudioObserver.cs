using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] private AudioClip phaseChangeClipAll;
    [SerializeField] private AudioClip gameClearClipAll;

    [SerializeField, Range(0f, 1f)] private float phaseVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float clearVolumeAll = 1f;

    private NGameManager managerAll;

    private void Start()
    {
        managerAll = ManagerLocator.Instance.AllGameManager;
        if (managerAll == null) return;

        managerAll.phaseManager.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
        managerAll.OnGameEndRpc += data =>OnGameFinished();
    }

    private void OnDestroy()
    {
        if (managerAll == null) return;

        managerAll.phaseManager.syncedPhaseIndex.OnValueChanged -= OnPhaseChanged;
        managerAll.OnGameEndRpc -= data => OnGameFinished();
    }

    private void OnPhaseChanged(int oldValue, int newValue)
    {
        if (newValue >= 0 && oldValue != newValue)
        {
            ManagerLocator.Instance.GameAudioManager?.PlayUI(phaseChangeClipAll, phaseVolumeAll);
        }
    }

    private void OnGameFinished()
    {
        ManagerLocator.Instance.GameAudioManager?.PlayUI(gameClearClipAll, clearVolumeAll);
    }
}