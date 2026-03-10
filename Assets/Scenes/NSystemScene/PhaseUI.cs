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
            manager.countdownValue.OnValueChanged += OnCountdownChanged;
            manager.phaseFinishing.OnValueChanged += OnPhaseFinishingChanged;

            // 初期値チェック
            OnPhaseFinishingChanged(false, manager.phaseFinishing.Value);

            OnPhaseChanged(-1, manager.syncedPhaseIndex.Value);
            OnCountdownChanged(0, manager.countdownValue.Value);

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

    void OnCountdownChanged(int oldValue, int newValue)
    {
        if (newValue > 0)
        {
            phaseText.text = newValue.ToString();
            phaseBoard.SetActive(true);

            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), 1f); // 1秒で消す
        }
    }

    public void ShowPhaseFinish()
    {
        int score = NGameManager.Instance.GetScore();
        var manager = NGameManager.Instance;
        int phase = manager.syncedPhaseIndex.Value;

        phaseText.text = $"Phase {phase + 1} FINISH!\nScore: {score} point";
        phaseBoard.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 3f); // 3秒後に消す
    }
    void OnPhaseFinishingChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowPhaseFinish(); // フラグがtrueになったら呼ぶ
        }
    }
}