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
    [SerializeField] BoolEvent WarningStateEvent;
    [Header("Reference")]
    [SerializeField] DifficultyDataBase rpcDataBase;
    [SerializeField] private NetworkGameManager nGameManager;
    [SerializeField] TMP_FontAsset japaneseFont;
    [SerializeField] TMP_FontAsset englishFont;
    [SerializeField] TMP_FontAsset warningFont;
    [SerializeField] LoopScrollUI[] warningScrollers;

    private Coroutine currentRoutine;
    private int currentPhaseIndex;
    private bool isCountdownSubscribed = false;

    bool isJapanese;

    enum UIState
    {
        Idle,
        PhaseIntro,
        Countdown,
        PhaseFinish,
        GameFinish,
        AllEnemyDead,
        Warning
    }

    private UIState currentState = UIState.Idle;
    private void Start()
    {
        UpdateLangage();
    }
    /// <summary>
    /// 言語設定を更新します。Startは一回しか呼ばれない。
    /// </summary>
    void UpdateLangage()
    {
        isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";
        ApplyFont();
        InitializeUI();
    }

    void ApplyFont()
    {
        phaseText.font = isJapanese ? japaneseFont : englishFont;
    }

    void InitializeUI()
    {
        phaseText.gameObject.SetActive(false);
        phaseBoard.SetActive(false);
        // CountdownValue の購読は OnPhaseChanged が来てから行う
        // Start() 時点では PhaseManager がまだスポーンしていない可能性がある
    }

    // CountdownValue の購読を安全に行うメソッド
    void TrySubscribeCountdown()
    {
        if (isCountdownSubscribed) return;
        if (nGameManager == null || nGameManager.PhaseManager == null) return;

        nGameManager.PhaseManager.CountdownValue.OnValueChanged += OnCountdownChanged;
        isCountdownSubscribed = true;
        Debug.Log("[PhaseUI] CountdownValue subscribed");
    }

    private void OnEnable()
    {
        AllEnemyDeadRpcEvent.Register(OnAllEnemyKilled);
        OnPhaseChangeRpcEvent.Register(OnPhaseChanged);
        GameStateChangeRpcEvent.Register(OnGameStateChanged);
        WarningStateEvent.Register(OnWarningChanged);
    }

    private void OnDisable()
    {
        AllEnemyDeadRpcEvent.Unregister(OnAllEnemyKilled);
        OnPhaseChangeRpcEvent.Unregister(OnPhaseChanged);
        GameStateChangeRpcEvent.Unregister(OnGameStateChanged);
        WarningStateEvent.Unregister(OnWarningChanged);

        // CountdownValue の購読解除
        if (isCountdownSubscribed && nGameManager != null && nGameManager.PhaseManager != null)
        {
            nGameManager.PhaseManager.CountdownValue.OnValueChanged -= OnCountdownChanged;
            isCountdownSubscribed = false;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver || newState == GameState.GameClear)
        {
            ChangeState(UIState.GameFinish);
        }
        else
        {
            //ゲーム状態が変わった場合も呼ぶ
            UpdateLangage();
        }
    }

    private void OnAllEnemyKilled()
    {
        ChangeState(UIState.AllEnemyDead);
    }

    // =========================
    // ステート管理
    // =========================
    void ChangeState(UIState next, object payload = null)
    {
        Debug.Log($"[PhaseUI][State] {currentState} → {next}");

        // PhaseIntro は同じ状態でも必ず実行する（フェーズ番号が変わるため）
        if (currentState == next && next != UIState.Countdown && next != UIState.PhaseIntro)
        {
            Debug.Log($"[PhaseUI][State] Skipped (same state): {next}");
            return;
        }

        // 現在の演出停止
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentState = next;

        switch (next)
        {
            case UIState.PhaseIntro:
                currentRoutine = StartCoroutine(PhaseIntroRoutine(((int, string))payload));
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

            case UIState.Warning:
                currentRoutine = StartCoroutine(WarningRoutine());
                break;

            case UIState.Idle:
                Hide();
                break;
        }
    }

    // =========================
    // イベント受信
    // =========================
    void OnPhaseChanged(int index)
    {
        Debug.Log($"[PhaseUI] OnPhaseChanged called index:{index}");

        // CountdownValue の購読をここで安全に行う
        TrySubscribeCountdown();

        currentPhaseIndex = index;
        if (currentPhaseIndex < 0 || currentPhaseIndex >= rpcDataBase.CurrentSetting.Phases.Length)
        {
            Debug.Log($"[PhaseUI] OnPhaseChanged index out of range: {index}");
            return;
        }

        var phaseSetting = rpcDataBase.CurrentSetting.Phases[currentPhaseIndex];

        if (phaseSetting != null)
        {
            string text = isJapanese ? phaseSetting.PhaseDisplayNameJP : phaseSetting.PhaseDisplayNameEN;
            ChangeState(UIState.PhaseIntro, (index, text));
        }
    }

    void OnCountdownChanged(int oldValue, int newValue)
    {
        if (currentState == UIState.GameFinish) return;

        if (newValue <= 0)
        {
            if (currentState == UIState.Countdown)
            {
                ChangeState(UIState.Idle);
            }
            return;
        }

        if (currentState == UIState.Countdown)
        {
            phaseText.text = newValue.ToString();
            StartCoroutine(PopAnimation());
            return;
        }

        ChangeState(UIState.Countdown, newValue);
    }

    // =========================
    // 各ステート処理
    // =========================

    IEnumerator PhaseIntroRoutine((int index, string text) data)
    {
        SetupNormal();

        int index = data.index;
        string text = data.text;

        var phaseSetting = rpcDataBase.CurrentSetting;
        int lastIndex = phaseSetting.Phases.Length - 1;

        phaseText.fontSize = 120;

        // 最初のフェーズ（基本戦）の開始表示
        if (index == 0)
        {
            phaseText.text = isJapanese ? "任務開始！" : "Mission Start!";
            phaseText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);

            // phaseText.text = isJapanese ? "戦闘開始" : "Combat Phase";
            // yield return new WaitForSeconds(1f);
        }
        // 最終フェーズ（ボス戦）の開始表示
        else if (index == lastIndex)
        {
            phaseText.text = isJapanese ? "ボス戦突入！" : "Boss Battle!";
            phaseText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
        }
        // 中間フェーズがある場合はフェーズ名をそのまま表示
        else
        {
            phaseText.text = text;
            phaseText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
        }

        phaseText.fontSize = 150;
        phaseText.text = isJapanese ? "スタート!!" : "START!!";

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
    }

    IEnumerator PhaseFinishRoutine()
    {
        int score = nGameManager.HPManager.GetHP();

        SetupNormal();

        phaseText.text = $"Phase {currentPhaseIndex + 1} FINISH!\nScore: {score}";
        phaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        ChangeState(UIState.Idle);
    }

    IEnumerator GameFinishRoutine()
    {
        SetupNormal();

        phaseText.fontSize = 150;
        phaseText.text = isJapanese ? "任務完了！" : "Mission Complete!";
        phaseText.gameObject.SetActive(true);

        yield return PopAnimation();

        yield return new WaitForSeconds(2f);

        ChangeState(UIState.Idle);
    }

    IEnumerator AllEnemyDeadRoutine()
    {
        int bonus = rpcDataBase.CurrentSetting.Phases[currentPhaseIndex].ClearBonus;

        SetupNormal();

        phaseText.fontSize = 100;
        phaseText.text = isJapanese ? "全敵撃破！" : "ALL ENEMY DEAD!";
        phaseText.gameObject.SetActive(true);

        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        phaseText.text = isJapanese ? "クリアボーナス" : "CLEAR BONUS";
        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        phaseText.text = isJapanese ? $"スコア: +{bonus}" : $"SCORE: +{bonus}";
        yield return PopAnimation();
        yield return new WaitForSeconds(1f);

        ChangeState(UIState.Idle);
    }

    // =========================
    // 共通処理
    // =========================

    void SetupNormal()
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", 0f);
        phaseText.font = isJapanese ? japaneseFont : englishFont;
        phaseText.color = Color.white;
    }

    void Hide()
    {
        phaseText.gameObject.SetActive(false);
        phaseBoard.gameObject.SetActive(false);
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
        float invduration = 1f / duration;

        while (t < duration)
        {
            t += Time.deltaTime;
            phaseText.transform.localScale =
                Vector3.Lerp(big, normal, t * invduration);
            yield return null;
        }

        phaseText.transform.localScale = normal;
    }

    IEnumerator WarningRoutine()
    {
        phaseText.font = warningFont;
        phaseText.fontSize = 180;
        phaseText.color = Color.black;
        phaseText.text = "WARNING!";
        phaseText.gameObject.SetActive(true);
        phaseBoard.gameObject.SetActive(true);

        foreach (var scroller in warningScrollers)
        {
            scroller.StartScroll();
        }

        for (int i = 0; i < 5; i++)
        {
            phaseText.enabled = false;
            yield return new WaitForSeconds(0.4f);

            phaseText.enabled = true;
            yield return new WaitForSeconds(0.4f);
        }

        phaseText.enabled = true;
        yield return new WaitForSeconds(0.5f);

        foreach (var scroller in warningScrollers)
        {
            scroller.StopScroll();
        }

        ChangeState(UIState.Idle);
    }

    void OnWarningChanged(bool active)
    {
        if (active)
        {
            ChangeState(UIState.Warning);
        }
        else
        {
            if (currentState != UIState.Countdown)
            {
                ChangeState(UIState.Idle);
            }
        }
    }
}
