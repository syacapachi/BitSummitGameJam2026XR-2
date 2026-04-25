using Syacapachi.Attribute;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [System.Serializable]
    class SkyBoxSetting
    {
        [Header("空の真上の色")]
        public Color topColor;
        [Header("空の基本色")]
        public Color horizonColor;
        [Header("空の地平線の色")]
        public Color bottomColor;
        [Header("空の明るさ(0~1)")]
        public float intensity;
        [Header("空の真上の色の浸食度")]
        public float exponentTop;
        [Header("空の地平線の色の浸食度")]
        public float exponentBottom;
    }
    [Header("SkyBox Material")]
    [SerializeField] Material skyboxMat;
    [SerializeField] SkyBoxSetting[] skyBoxColors;
    [SerializeField] int defaultIndex = 0;
    [Header("Subscribe Event")]
    [SerializeField] IntEvent phaseChageEvent;

    private void Awake()
    {
        ApplyColor(defaultIndex);
    }
    private void OnEnable()
    {
        phaseChageEvent.Register(ApplyColor);
    }
    private void OnDisable()
    {
        phaseChageEvent.Unregister(ApplyColor);
    }
    /// <summary>
    /// 即時変更用。
    /// </summary>
    /// <param name="index"></param>
    [OnInspectorButton("仮適応ボタン")]
    void ApplyColor(int index)
    {
        if (index < 0 || index >= skyBoxColors.Length) return;
        var c = skyBoxColors[index];
        skyboxMat.SetColor("_Color1", c.topColor);
        skyboxMat.SetColor("_Color2", c.horizonColor);
        skyboxMat.SetColor("_Color3", c.bottomColor);

        skyboxMat.SetFloat("_Intensity", c.intensity);
        skyboxMat.SetFloat("_Exponent1", c.exponentTop);
        skyboxMat.SetFloat("_Exponent2", c.exponentBottom);
    }
    /// <summary>
    /// 徐々に変化させる用。Update等で呼び出すことを想定。
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    void ApplyLerp(SkyBoxSetting a, SkyBoxSetting b, float t)
    {
        skyboxMat.SetColor("_Color1", Color.Lerp(a.topColor, b.topColor, t));
        skyboxMat.SetColor("_Color2", Color.Lerp(a.horizonColor, b.horizonColor, t));
        skyboxMat.SetColor("_Color3", Color.Lerp(a.bottomColor, b.bottomColor, t));

        skyboxMat.SetFloat("_Intensity", Mathf.Lerp(a.intensity, b.intensity, t));
        skyboxMat.SetFloat("_Exponent1", Mathf.Lerp(a.exponentTop, b.exponentTop, t));
        skyboxMat.SetFloat("_Exponent2", Mathf.Lerp(a.exponentBottom, b.exponentBottom, t));
    }
}
