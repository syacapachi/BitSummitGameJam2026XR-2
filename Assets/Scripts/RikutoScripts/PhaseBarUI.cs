using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PhaseBarUI : MonoBehaviour
{
    [Header("Depend On")]
    [SerializeField] DifficultyDataBase rpcDataBase;
    [SerializeField] PhaseManager phaseManager;

    [Header("UI")]
    [SerializeField] Canvas timerCanvas;
    [SerializeField] GameObject phaseBarPrefab;
    [SerializeField] GameObject separatorPrefab;
    [SerializeField] Transform container;

    [Header("Bar Size")]
    [SerializeField] float fixedBarWidth = 300f;

    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    [SerializeField] IntEvent onPhaseChangeRpcEvent;

    private Image currentBar;
    private Image currentSeparator;

    private Coroutine progressCoroutine;

    private Color defaultColor;

    private GameState currentState;

    // =========================================================
    // Unity Event
    // =========================================================

    private void OnEnable()
    {
        gameStateEvent.Register(OnStateChange);
        onPhaseChangeRpcEvent.Register(OnPhaseChanged);
    }

    private void OnDisable()
    {
        gameStateEvent.Unregister(OnStateChange);
        onPhaseChangeRpcEvent.Unregister(OnPhaseChanged);
    }

    // =========================================================
    // GameState
    // =========================================================

    private void OnStateChange(GameState state)
    {
        currentState = state;

        switch (state)
        {
            case GameState.Playing:
                timerCanvas.enabled = true;
                break;

            case GameState.GameOver:
            case GameState.GameClear:
            case GameState.Initializing:
            case GameState.Home:

                timerCanvas.enabled = false;

                ClearCurrentBar();

                break;
        }
    }

    // =========================================================
    // Phase Change
    // =========================================================

    private void OnPhaseChanged(int phaseIndex)
    {
        if (phaseIndex < 0)
            return;

        CreatePhaseBar(phaseIndex);

        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
        }

        progressCoroutine = StartCoroutine(PhaseProgress());
    }

    // =========================================================
    // Create
    // =========================================================

    private void CreatePhaseBar(int phaseIndex)
    {
        ClearCurrentBar();

        //PhaseSO phase =
        //    rpcDataBase.CurrentSetting.Phases[phaseIndex];

        // =====================================
        // Separator
        // =====================================

        if (separatorPrefab != null)
        {
            GameObject sepObj =
                Instantiate(separatorPrefab, container);

            currentSeparator =
                sepObj.GetComponent<Image>();
        }

        // =====================================
        // Bar
        // =====================================

        GameObject barObj =
            Instantiate(phaseBarPrefab, container);

        currentBar =
            barObj.GetComponent<Image>();

        defaultColor = currentBar.color;

        // =====================================
        // Width
        // =====================================

        RectTransform rt =
            currentBar.rectTransform;

        rt.sizeDelta = new Vector2(
            fixedBarWidth,
            rt.sizeDelta.y
        );

        currentBar.fillAmount = 1f;
        currentBar.color = Color.yellow;
    }

    // =========================================================
    // Destroy
    // =========================================================

    private void ClearCurrentBar()
    {
        if (currentBar != null)
        {
            Destroy(currentBar.gameObject);
            currentBar = null;
        }

        if (currentSeparator != null)
        {
            Destroy(currentSeparator.gameObject);
            currentSeparator = null;
        }
    }

    // =========================================================
    // Progress
    // =========================================================

    private IEnumerator PhaseProgress()
    {
        while (currentState == GameState.Playing)
        {
            if (currentBar == null)
            {
                yield break;
            }

            float progress =
                phaseManager.phaseProgress.Value;

            currentBar.fillAmount = progress;

            // 現在フェーズ
            currentBar.color = Color.yellow;

            // フェーズ終了
            if (progress <= 0f)
            {
                break;
            }

            yield return null;
        }

        ClearCurrentBar();
    }
}

/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhaseBarUI : MonoBehaviour
{
    [Header("depend on")]
    [SerializeField] DifficultyDataBase rpcDataBase;
    [SerializeField] PhaseManager phaseManager;
    [Header("UI")]
    [SerializeField] Canvas timerCanvas;
    [SerializeField] GameObject phaseBarPrefab;
    [SerializeField] GameObject separatorPrefab;
    [SerializeField] Transform container;

    [Header("Size")]
    [SerializeField] RectTransform rect;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent GameStateEvent;
    float MaxWidth => rect.rect.width; // 最大フェーズの長さ

    private readonly List<Image> phaseBars = new List<Image>();
    private readonly List<Image> separators = new List<Image>();

    private Color defaultColor;
    GameState currentState;
    private void OnEnable()
    {
        GameStateEvent.Register(OnStateChange);
    }
    private void OnDisable()
    {
        GameStateEvent.Unregister(OnStateChange);
    }
    private void OnStateChange(GameState state)
    {
        currentState = state;
        if (state == GameState.Playing)
        {
            CreateBars();
            SetupBarLength();
            StartCoroutine(PhaseProgress());
        }
        if (state == GameState.GameOver || state == GameState.GameClear)
        {
            timerCanvas.enabled = false;
        }
    }
    // =========================
    // フェーズバー生成
    // =========================
    void CreateBars()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        phaseBars.Clear();
        separators.Clear();

        int count = rpcDataBase.CurrentSetting.Phases.Length;

        for (int i = 0; i < count; i++)
        {
            GameObject sepObj = Instantiate(separatorPrefab, container);
            Image sepImg = sepObj.GetComponent<Image>();
            separators.Add(sepImg);
            GameObject obj = Instantiate(phaseBarPrefab, container);
            Image img = obj.GetComponent<Image>();

            phaseBars.Add(img);

            if (i == 0)
            {
                defaultColor = img.color;
            }
        }
    }

    // =========================
    // バーの長さ設定
    // =========================
    void SetupBarLength()
    {
        float maxTime = 0f;
        // 最大時間取得
        foreach (var phase in rpcDataBase.CurrentSetting.Phases)
        {
            maxTime += phase.PhaseTime;
        }

        // 各バーに反映
        for (int i = 0; i < phaseBars.Count; i++)
        {
            float time = rpcDataBase.CurrentSetting.Phases[i].PhaseTime;
            float ratio = time / maxTime;

            int visualIndex = phaseBars.Count - 1 - i;

            RectTransform rt = phaseBars[visualIndex].rectTransform;
            rt.sizeDelta = new Vector2(MaxWidth * ratio, rt.sizeDelta.y);
        }
    }
    IEnumerator PhaseProgress()
    {
        //できればマネージャークラスへの依存を切りたい
        yield return new WaitWhile(() => phaseManager.CurrentPhaseIndex < 0);
        while(currentState == GameState.Playing)
        {
            int current = phaseManager.CurrentPhaseIndex;
            float progress = phaseManager.phaseProgress.Value;

            for (int i = 0; i < phaseBars.Count; i++)
            {
                int visualIndex = phaseBars.Count - 1 - i;
                if (i < current)
                {
                    // 過去フェーズ → 完了
                    phaseBars[visualIndex].fillAmount = 0f;
                    phaseBars[visualIndex].color = Color.gray;
                    separators[visualIndex].enabled = false;
                }
                else if (i == current)
                {
                    // 現在フェーズ → 減少
                    phaseBars[visualIndex].fillAmount = progress;
                    phaseBars[visualIndex].color = Color.yellow;
                    separators[visualIndex].enabled = true;
                }
                else
                {
                    // 未来フェーズ → フル
                    phaseBars[visualIndex].fillAmount = 1f;
                    phaseBars[visualIndex].color = defaultColor;
                    separators[visualIndex].enabled = true;
                }
            }
            yield return null;
        }
    }
}
*/