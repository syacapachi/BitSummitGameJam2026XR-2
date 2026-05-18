using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [Header("UI")]
    [SerializeField] GameObject root;
    [SerializeField] TextMeshProUGUI text; // ← 1つだけ

    [Header("操作説明画像")]
    [SerializeField] private GameObject operationGuideImage;

    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
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

    private void Start()
    {
        root.SetActive(false);
        if (operationGuideImage != null)
            operationGuideImage.SetActive(false);
    }

    void OnEnable()
    {
        //OnTutorialStepChanged.Register(OnStepChanged);
        OnTutorialStepCleared.Register(OnStepCleared);
        tutorialManager.CurrentStep.OnValueChanged += OnStepChangedNetwork;
        gameStateEvent.Register(OnStateChange);
    }

    void OnDisable()
    {
        //OnTutorialStepChanged.Unregister(OnStepChanged);
        OnTutorialStepCleared.Unregister(OnStepCleared);
        tutorialManager.CurrentStep.OnValueChanged -= OnStepChangedNetwork;
        gameStateEvent.Unregister(OnStateChange);
    }

    void OnStepChangedNetwork(TutorialStep oldStep, TutorialStep newStep)
    {
        Debug.Log($"[UI] StepChangedNetwork: {oldStep} → {newStep} / currentState={currentState}");

        if (currentState == TutorialUIState.StepClear)
            return;
        ChangeState(TutorialUIState.StepIntro, newStep);
    }

    void OnStateChange(GameState state)
    {
        if (state == GameState.Tutorial)
        {
            isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
            root.SetActive(false);

            // チュートリアル開始時に操作説明画像を表示
            if (operationGuideImage != null)
                operationGuideImage.SetActive(true);

            if (tutorialManager != null)
            {
                OnStepChangedNetwork(default, tutorialManager.CurrentStep.Value);
            }
        }
        else
        {
            // チュートリアル以外では操作説明画像を非表示
            if (operationGuideImage != null)
                operationGuideImage.SetActive(false);
        }
    }

    // =========================
    // 状態管理
    // =========================
    void ChangeState(TutorialUIState next, object payload = null)
    {
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
                desc = isJapanese ? "マーカーを置いて敵の位置を示せ！" : "Place a marker to indicate the enemy's position!";
                break;

            case TutorialStep.Step4:
                title = isJapanese ? "チュートリアル4" : "Tutorial 4";
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
        yield return new WaitForSeconds(1f);
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
        // 操作説明画像はチュートリアル中は非表示にしない
    }

    IEnumerator PopAnimation()
    {
        Vector3 normal = Vector3.one;
        Vector3 big = Vector3.one * 1.3f;

        text.transform.localScale = big;

        float t = 0f;
        float duration = 0.1f;
        float invduration = 1f / duration;
        while (t < duration)
        {
            t += Time.deltaTime;
            text.transform.localScale =
                Vector3.Lerp(big, normal, t * invduration);
            yield return null;
        }

        text.transform.localScale = normal;
    }

    IEnumerator GrowAnimation()
    {
        Vector3 start = Vector3.one;
        Vector3 end = Vector3.one * 1.1f;

        float duration = 2f;
        float invDuration = 1f / duration;
        float t = 0f;

        text.transform.localScale = start;

        while (t < duration)
        {
            t += Time.deltaTime;

            float progress = t * invDuration;
            float ease = Mathf.SmoothStep(0f, 1f, progress);

            text.transform.localScale = Vector3.Lerp(start, end, ease);

            yield return null;
        }

        text.transform.localScale = end;
    }
}
