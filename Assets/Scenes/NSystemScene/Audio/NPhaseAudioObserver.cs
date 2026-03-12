using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] private AudioClip phaseChangeClip;
    [SerializeField] private AudioClip gameClearClip;

    [SerializeField, Range(0f, 1f)] private float phaseVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float clearVolume = 1f;

    private NGameManager manager;

    private void Start()
    {
        manager = ManagerLocator.Instance.GameManager;
        if (manager == null) return;

        manager.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
        manager.IsGameFinished.OnValueChanged += OnGameFinishedChanged;
    }

    private void OnDestroy()
    {
        if (manager == null) return;

        manager.syncedPhaseIndex.OnValueChanged -= OnPhaseChanged;
        manager.IsGameFinished.OnValueChanged -= OnGameFinishedChanged;
    }

    private void OnPhaseChanged(int oldValue, int newValue)
    {
        if (newValue >= 0 && oldValue != newValue)
        {
            GameAudioManager.Instance?.PlayUI(phaseChangeClip, phaseVolume);
        }
    }

    private void OnGameFinishedChanged(bool oldValue, bool newValue)
    {
        if (!oldValue && newValue)
        {
            GameAudioManager.Instance?.PlayUI(gameClearClip, clearVolume);
        }
    }
}