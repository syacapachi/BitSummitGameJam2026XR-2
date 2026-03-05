using UnityEngine;
using TMPro;

public class PhaseUI : MonoBehaviour
{
    public GameObject phaseBoard;
    public TextMeshProUGUI phaseText;

    void Start()
    {
        if (NGameManager.Instance != null)
        {
            var manager = NGameManager.Instance;

            manager.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
            manager.IsGameFinished.OnValueChanged += OnGameFinishedChanged;

            OnPhaseChanged(-1, manager.syncedPhaseIndex.Value);

            if (manager.IsGameFinished.Value)
            {
                ShowScore();
            }
        }
    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        var manager = NGameManager.Instance;
        if (manager == null) return;

        if (newValue >= 0 && newValue < manager.phases.Length)
        {
            string text = manager.phases[newValue].phaseDisplayName;
            Show(text);
        }
    }

    void Show(string text)
    {
        phaseText.text = text;
        phaseBoard.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 3f);
    }

    void Hide()
    {
        phaseBoard.SetActive(false);
    }

    void ShowScore()
    {
        int score = NGameManager.Instance.GetScore();

        phaseText.text = $"Score : {score} point";
        phaseBoard.SetActive(true);

        CancelInvoke(nameof(Hide));
    }

    void OnGameFinishedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowScore();
        }
    }
}