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
        managerAll = ManagerLocator.Instance.GameManager;
        if (managerAll == null) return;

        managerAll.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
        managerAll.IsGameFinished.OnValueChanged += OnGameFinishedChanged;
    }

    private void OnDestroy()
    {
        if (managerAll == null) return;

        managerAll.syncedPhaseIndex.OnValueChanged -= OnPhaseChanged;
        managerAll.IsGameFinished.OnValueChanged -= OnGameFinishedChanged;
    }

    private void OnPhaseChanged(int oldValue, int newValue)
    {
        if (newValue >= 0 && oldValue != newValue)
        {
            GameAudioManager.Instance?.PlayUI(phaseChangeClipAll, phaseVolumeAll);
        }
    }

    private void OnGameFinishedChanged(bool oldValue, bool newValue)
    {
        if (!oldValue && newValue)
        {
            GameAudioManager.Instance?.PlayUI(gameClearClipAll, clearVolumeAll);
        }
    }
}