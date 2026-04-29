using Syacapachi.Attribute;
using System.Collections;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [System.Serializable]
    struct SkyBoxSetting
    {
        [Header("空の真上の色")]
        public Color topColor;
        [Header("空の基本色")]
        public Color horizonColor;
        [Header("空の地平線の色")]
        public Color bottomColor;
        [Header("空の明るさ")]
        public float intensity;
        [Header("空の真上の色の浸食度")]
        public float exponentTop;
        [Header("空の地平線の色の浸食度")]
        public float exponentBottom;
    }
    [SerializeField] float changeTime = 5f;
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
        phaseChageEvent.Register(ApplyWithCorutine);
    }
    private void OnDisable()
    {
        phaseChageEvent.Unregister(ApplyWithCorutine);
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
    [OnInspectorButton("仮適応コルーチンボタン",showOnlyInPlayMode = true)]
    private void ApplyWithCorutine(int index)
    {
        if(index == 0)
        {
            StartCoroutine(ApplyColorCorutine(skyBoxColors.Length -1, index, changeTime));
            return;
        }
        if(index < 1 || index >= skyBoxColors.Length) return;
        StartCoroutine(ApplyColorCorutine(index-1,index, changeTime));
    }
    IEnumerator ApplyColorCorutine(int fromIndex,int toIndex,float changeTime)
    {
        for(float timer = 0f; timer <= changeTime; timer += Time.deltaTime)
        {
            ApplyLerp(skyBoxColors[fromIndex], skyBoxColors[toIndex], timer / changeTime);
            yield return null;
        }
        ApplyColor(toIndex);
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
