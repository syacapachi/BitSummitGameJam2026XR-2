using UnityEngine;
using TMPro;
using System.Collections;

public class PhaseUI : MonoBehaviour
{
    [SerializeField] GameObject phaseBoard;
    [SerializeField] TextMeshProUGUI phaseText;
    [SerializeField] PhaseCountDownSettingSO phaseUISettingSO;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent AllEnemyDeadRpcEvent;
    [SerializeField] IntEvent OnPhaseChangeRpcEvent;
    [SerializeField] GameStateEvent GameStateChangeRpcEvent;
    [SerializeField] private NetworkGameManager nGameManager;

    private Coroutine currentRoutine;

    enum UIState
    {
        Idle,
        PhaseIntro,
        Countdown,
        PhaseFinish,
        GameFinish,
        AllEnemyDead
    }

    private UIState currentState = UIState.Idle;

    void Start()
    {
        InitializeUI();
    }

    void InitializeUI()
    {
        phaseText.gameObject.SetActive(false);

        nGameManager.PhaseManager.CountdownValue.OnValueChanged += OnCountdownChanged; 
    }
    private void OnEnable()
    {
        AllEnemyDeadRpcEvent.Register(OnAllEnemyKilled);
        OnPhaseChangeRpcEvent.Register(OnPhaseChanged);
        GameStateChangeRpcEvent.Register(OnGameStateChanged);
    }
    private void OnDisable()
    {
        AllEnemyDeadRpcEvent.Unregister(OnAllEnemyKilled);
        OnPhaseChangeRpcEvent.Unregister(OnPhaseChanged);
        GameStateChangeRpcEvent.Unregister(OnGameStateChanged);
    }
    private void OnGameStateChanged(GameState newState)
    {
        if(newState == GameState.GameOver || newState == GameState.GameClear)
        {
            ChangeState(UIState.GameFinish);
        }
    }
    private void OnAllEnemyKilled()
    {
        ChangeState(UIState.AllEnemyDead);
    }
    // =========================
    // 🔥 ステート管理
    // =========================
    void ChangeState(UIState next, object payload = null)
    {
        if (currentState == next && next != UIState.Countdown) return;
        // 同じ状態は無視（必要なら消す）
        if (currentState == next) return;

        // 現在の演出停止
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentState = next;

        switch (next)
        {
            case UIState.PhaseIntro:
                currentRoutine = StartCoroutine(PhaseIntroRoutine((string)payload));
                break;

            case UIState.Countdown:
                currentRoutine = StartCoroutine(CountdownRoutine((int)payload));
                break;

            case UIState.PhaseFinish:
                currentRoutine = StartCoroutine(PhaseFinishRoutine());
                break;

            case UIState.GameFinish:
                currentRoutine = StartCoroutine(GameFinishRoutine());
                break;

            case UIState.AllEnemyDead:
                currentRoutine = StartCoroutine(AllEnemyDeadRoutine());
                break;

            case UIState.Idle:
                Hide();
                break;
        }
    }

    // =========================
    // 🎯 イベント受信
    // =========================
    void OnPhaseChanged(int index)
    {
        var manager = nGameManager.PhaseManager;
        if (index >= 0 && index < manager.Phases.Length)
        {
            string text = manager.Phases[index].PhaseDisplayName;
            ChangeState(UIState.PhaseIntro, text);
        }
    }

    void OnCountdownChanged(int oldValue, int newValue)
    {
        if (currentState == UIState.GameFinish) return;
        if (newValue <= 0) return;
        if (currentState == UIState.Countdown)
        {
            phaseText.text = newValue.ToString();
            StartCoroutine(PopAnimation());
            return;
        }

        ChangeState(UIState.Countdown, newValue);
    }

    // =========================
    // 🎬 各ステート処理
    // =========================

    IEnumerator PhaseIntroRoutine(string text)
    {
        SetupNormal();

        phaseText.fontSize = 120;
        phaseText.text = text;
        phaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);

        phaseText.fontSize = 150;
        phaseText.text = "START!!";
        yield return PopAnimation();

        yield return new WaitForSeconds(1.5f);

        ChangeState(UIState.Idle);
    }

    IEnumerator CountdownRoutine(int index)
    {
        SetupNormal();


        phaseText.text = index.ToString();
        phaseText.gameObject.SetActive(true);
        yield return PopAnimation();

        //ChangeState(UIState.Idle);
    }

    IEnumerator PhaseFinishRoutine()
    {
        int score = nGameManager.ScoreManager.GetScore();
        int phase = nGameManager.PhaseManager.CurrentPhaseIndex;

        SetupNormal();

        phaseText.text = $"Phase {phase + 1} FINISH!\nScore: {score}";
        phaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        ChangeState(UIState.Idle);
    }

    IEnumerator GameFinishRoutine()
    {
        SetupNormal();

        phaseText.fontSize = 150;
        phaseText.text = "FINISH!";
        phaseText.gameObject.SetActive(true);

        yield return PopAnimation();

        yield return new WaitForSeconds(2f);

        ChangeState(UIState.Idle);
    }

    IEnumerator AllEnemyDeadRoutine()
    {
        int bonus = nGameManager.ScoreManager.LastClearBonus;

        SetupNormal();

        phaseText.fontSize = 100;
        phaseText.text = "ALL ENEMY DEAD!";
        phaseText.gameObject.SetActive(true);

        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        phaseText.text = "CLEAR BONUS";
        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        phaseText.text = $"SCORE: +{bonus}";
        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        ChangeState(UIState.Idle);
    }

    // =========================
    // 🎨 共通処理
    // =========================

    void SetupNormal()
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", 0f);
    }

    void Hide()
    {
        phaseText.gameObject.SetActive(false);
    }

    IEnumerator PopAnimation()
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 1f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", -1f);

        Vector3 normal = Vector3.one;
        Vector3 big = Vector3.one * 1.6f;

        phaseText.transform.localScale = big;

        float t = 0f;
        float duration = 0.05f;

        while (t < duration)
        {
            t += Time.deltaTime;
            phaseText.transform.localScale =
                Vector3.Lerp(big, normal, t / duration);
            yield return null;
        }

        phaseText.transform.localScale = normal;
    }
}