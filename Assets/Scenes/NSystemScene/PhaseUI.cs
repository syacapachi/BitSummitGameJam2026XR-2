using UnityEngine;
using TMPro;

public class PhaseUI : MonoBehaviour
{
    public GameObject phaseBoard;
    public TextMeshProUGUI phaseText;
    private NGameManager nGameManager;
    void Start()
    {
        nGameManager = ManagerLocator.Instance.NGameManager;
        if (nGameManager != null)
        {

            // �����l�`�F�b�N
            OnPhaseFinishingChanged(false, nGameManager.phaseFinishing.Value);

            OnPhaseChanged(-1, nGameManager.syncedPhaseIndex.Value);
            OnCountdownChanged(0, nGameManager.countdownValue.Value);

            nGameManager.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
            nGameManager.IsGameFinished.OnValueChanged += OnGameFinishedChanged;
            nGameManager.countdownValue.OnValueChanged += OnCountdownChanged;
            nGameManager.phaseFinishing.OnValueChanged += OnPhaseFinishingChanged;

            OnPhaseChanged(-1, nGameManager.syncedPhaseIndex.Value);

            if (nGameManager.IsGameFinished.Value)
            {
                ShowScore();
            }
        }
    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        var manager = nGameManager;
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
        int score = nGameManager.GetScore();

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

    void OnCountdownChanged(int oldValue, int newValue)
    {
        if (newValue > 0)
        {
            phaseText.text = newValue.ToString();
            phaseBoard.SetActive(true);

            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), 1f); // 1�b�ŏ���
        }
    }

    public void ShowPhaseFinish()
    {
        int score = nGameManager.GetScore();
        int phase = nGameManager.syncedPhaseIndex.Value;

        phaseText.text = $"Phase {phase + 1} FINISH!\nScore: {score} point";
        phaseBoard.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 3f); // 3�b���ɏ���
    }
    void OnPhaseFinishingChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowPhaseFinish(); // �t���O��true�ɂȂ������Ă�
        }
    }
}