using UnityEngine;

public class NPhaseAudioObserver : MonoBehaviour
{
    [SerializeField] private AudioClip phaseChangeClipAll;
    [SerializeField] private AudioClip gameClearClipAll;

    [SerializeField, Range(0f, 1f)] private float phaseVolumeAll = 1f;
    [SerializeField, Range(0f, 1f)] private float clearVolumeAll = 1f;

    private NGameManager managerAll;

    private void OnEnable()
    {
        managerAll = ManagerLocator.Instance.AllGameManager;
        if (managerAll == null) return;

        managerAll.PhaseManager.OnPhaseChange += OnPhaseChanged;
        managerAll.OnGameResultRpc += data => OnGameFinished();
    }

    private void OnDisable()
    {
        if (managerAll == null) return;

        managerAll.PhaseManager.OnPhaseChange -= OnPhaseChanged;
        managerAll.OnGameResultRpc -= data => OnGameFinished();
    }

    private void OnPhaseChanged(int newValue)
    {
        if (newValue >= 0)
        {
            ManagerLocator.Instance.GameAudioManager?.PlayUI(phaseChangeClipAll, phaseVolumeAll);
        }
    }

    private void OnGameFinished()
    {
        ManagerLocator.Instance.GameAudioManager?.PlayUI(gameClearClipAll, clearVolumeAll);
    }
}