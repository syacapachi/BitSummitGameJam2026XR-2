using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [Header("UI")]
    [SerializeField] GameObject root;
    [SerializeField] TextMeshProUGUI text; // ← 1つだけ

    [Header("Event")]
    [SerializeField] IntEvent OnTutorialStepChanged;
    [SerializeField] VoidEvent OnTutorialStepCleared;

    private Coroutine currentRoutine;
    private bool isJapanese;

    enum TutorialUIState
    {
        Idle,
        StepIntro,
        StepClear
    }

    private TutorialUIState currentState = TutorialUIState.Idle;

    void Start()
    {
        isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        root.SetActive(false);

        if (tutorialManager != null)
        {
            OnStepChangedNetwork(default, tutorialManager.CurrentStep.Value);
        }
    }

    void OnEnable()
    {
        //OnTutorialStepChanged.Register(OnStepChanged);
        OnTutorialStepCleared.Register(OnStepCleared);
        tutorialManager.CurrentStep.OnValueChanged += OnStepChangedNetwork;
    }

    void OnDisable()
    {
        //OnTutorialStepChanged.Unregister(OnStepChanged);
        OnTutorialStepCleared.Unregister(OnStepCleared);
        tutorialManager.CurrentStep.OnValueChanged -= OnStepChangedNetwork;
    }

    void OnStepChangedNetwork(TutorialStep oldStep, TutorialStep newStep)
    {
        Debug.Log($"[UI] StepChangedNetwork: {oldStep} → {newStep} / currentState={currentState}");

        if (currentState == TutorialUIState.StepClear)
            return;
        ChangeState(TutorialUIState.StepIntro, newStep);
    }

    // =========================
    // 状態管理
    // =========================
    void ChangeState(TutorialUIState next, object payload = null)
    {
        Debug.Log($"[UI] ChangeState: {currentState} → {next}");
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentState = next;

        switch (next)
        {
            case TutorialUIState.StepIntro:
                currentRoutine = StartCoroutine(StepIntroRoutine((TutorialStep)payload));
                break;

            case TutorialUIState.StepClear:
                currentRoutine = StartCoroutine(StepClearRoutine());
                break;

            case TutorialUIState.Idle:
                Hide();
                break;
        }
    }

    // =========================
    // イベント
    // =========================
    void OnStepChanged(int stepIndex)
    {
        ChangeState(TutorialUIState.StepIntro, (TutorialStep)stepIndex);
    }

    void OnStepCleared()
    {
        ChangeState(TutorialUIState.StepClear);
    }

    // =========================
    // ステップ開始
    // =========================
    IEnumerator StepIntroRoutine(TutorialStep step)
    {
        Debug.Log($"[UI] StepIntro START: {step}");
        root.SetActive(true);

        string title = "";
        string desc = "";

        switch (step)
        {
            case TutorialStep.Step1:
                title = isJapanese ? "チュートリアル1" : "Tutorial 1";
                desc = isJapanese ? "全ての的を破壊せよ！" : "Destroy all targets!";
                break;

            case TutorialStep.Step2:
                title = isJapanese ? "チュートリアル2" : "Tutorial 2";
                desc = isJapanese ? "見えている敵を3回攻撃しろ！" : "Attack visible enemies 3 times!";
                break;

            case TutorialStep.Step3:
                title = isJapanese ? "チュートリアル3" : "Tutorial 3";
                desc = isJapanese ? "見えない敵を倒せ！" : "Defeat invisible enemies!";
                break;
        }

        // --- タイトル表示（1秒） ---
        text.text = title;
        yield return new WaitForSeconds(1f);

        // --- 内容表示（1.5秒） ---
        text.transform.localScale = Vector3.one;
        text.text = desc;
        yield return GrowAnimation();
        yield return new WaitForSeconds(1.5f);

        ChangeState(TutorialUIState.Idle);
    }

    // =========================
    // クリア表示
    // =========================
    IEnumerator StepClearRoutine()
    {
        root.SetActive(true);

        text.text = "SUCCEED!";
        yield return new WaitForSeconds(2f);

        ChangeState(TutorialUIState.Idle);
    }

    // =========================
    // 共通
    // =========================
    void Hide()
    {
        root.SetActive(false);
    }

    IEnumerator PopAnimation()
    {
        Vector3 normal = Vector3.one;
        Vector3 big = Vector3.one * 1.3f;

        text.transform.localScale = big;

        float t = 0f;
        float duration = 0.1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            text.transform.localScale =
                Vector3.Lerp(big, normal, t / duration);
            yield return null;
        }

        text.transform.localScale = normal;
    }

    IEnumerator GrowAnimation()
    {
        Vector3 start = Vector3.one;
        Vector3 end = Vector3.one * 1.1f; // 最終サイズ

        float duration = 2f; // ゆっくり感はここで調整
        float t = 0f;

        text.transform.localScale = start;

        while (t < duration)
        {
            t += Time.deltaTime;

            float progress = t / duration;

            // なめらかに（イージング）
            float ease = Mathf.SmoothStep(0f, 1f, progress);

            text.transform.localScale = Vector3.Lerp(start, end, ease);

            yield return null;
        }

        text.transform.localScale = end;
    }
}