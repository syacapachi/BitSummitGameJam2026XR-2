using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PhaseBarUI : MonoBehaviour
{
    [Header("depend on")]
    [SerializeField] PhaseManager phaseManager;
    [Header("UI")]
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

    private void OnEnable()
    {
        GameStateEvent.Register(OnStateChange);
    }
    private void OnDisable()
    {
        GameStateEvent.Unregister(OnStateChange);
    }

    void Update()
    {
        if (phaseManager == null) return;

        int current = phaseManager.CurrentPhaseIndex;
        float progress = phaseManager.phaseProgress.Value;

        if (current < 0) return;

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
    }
    private void OnStateChange(GameState state)
    {
        if (state == GameState.Playing)
        {
            CreateBars();
            SetupBarLength();
        }
    }
    // =========================
    // フェーズバー生成
    // =========================
    void CreateBars()
    {
        int count = phaseManager.Phases.Length;

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
        foreach (var phase in phaseManager.Phases)
        {
            maxTime += phase.PhaseTime;
        }

        // 各バーに反映
        for (int i = 0; i < phaseBars.Count; i++)
        {
            float time = phaseManager.Phases[i].PhaseTime;
            float ratio = time / maxTime;

            int visualIndex = phaseBars.Count - 1 - i;

            RectTransform rt = phaseBars[visualIndex].rectTransform;
            rt.sizeDelta = new Vector2(MaxWidth * ratio, rt.sizeDelta.y);
        }
    }
}