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
            NGameManager.Instance.syncedPhaseIndex
                .OnValueChanged += OnPhaseChanged;
        }
    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        var manager = NGameManager.Instance;

        if (manager == null) return;
        if (newValue < 0 || newValue >= manager.phases.Length) return;

        string text = manager.phases[newValue].phaseDisplayName;

        Show(text);
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
}