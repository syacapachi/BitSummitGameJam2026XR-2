using Meta.WitAi.Attributes;
using Syacapachi.Attribute;
using System.Collections;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [SerializeField] float changeTime = 5f;
    [Header("SkyBox Material")]
    [SerializeField] Material skyboxMat;
    [SerializeField] SkyBoxColorSetting[] skyBoxColorSettings;
    [SerializeField] int defaultIndex = 0;
    [SerializeField] Light[] directionalLights;
    [Header("Subscribe Event")]
    [SerializeField] IntEvent phaseChageEvent;
    [Header("デバックモード")]
    [SerializeField] bool IsDebug;
    [SerializeField, EnableIf(nameof(IsDebug))] VoidEvent debugEvent;
    private int currentIndex = 0;
    private void Awake()
    {
        ApplyColor(defaultIndex);
    }
    private void OnEnable()
    {
        phaseChageEvent.Register(ApplyWithCorutine);
        if(IsDebug)
        {
            debugEvent.Register(DebugApply);
        }
    }
    private void OnDisable()
    {
        phaseChageEvent.Unregister(ApplyWithCorutine);
        if(IsDebug)
        {
            debugEvent.Unregister(DebugApply);
        }
    }
    /// <summary>
    /// 即時変更用。
    /// </summary>
    /// <param name="index"></param>
    [OnInspectorButton("適応ボタン(インデックス)")]
    void ApplyColor(int index)
    {
        if (index < 0 || index >= skyBoxColorSettings.Length) return;
        var c = skyBoxColorSettings[index];
        skyboxMat.SetColor("_Color1", c.topColor);
        skyboxMat.SetColor("_Color2", c.horizonColor);
        skyboxMat.SetColor("_Color3", c.bottomColor);

        skyboxMat.SetFloat("_Intensity", c.intensity);
        skyboxMat.SetFloat("_Exponent1", c.exponentTop);
        skyboxMat.SetFloat("_Exponent2", c.exponentBottom);
    }
    [OnInspectorButton("適応ボタン(設定)")]
    void ApplyColor(SkyBoxColorSetting setting)
    {
        if (setting == null)
        {
            Debug.LogWarning("setting is null");
            return;
        }
        skyboxMat.SetColor("_Color1", setting.topColor);
        skyboxMat.SetColor("_Color2", setting.horizonColor);
        skyboxMat.SetColor("_Color3", setting.bottomColor); 
        skyboxMat.SetFloat("_Intensity", setting.intensity);
        skyboxMat.SetFloat("_Exponent1", setting.exponentTop);
        skyboxMat.SetFloat("_Exponent2", setting.exponentBottom);
    }
    private void DebugApply()
    {
        currentIndex = (currentIndex + 1) % skyBoxColorSettings.Length;
        ApplyWithCorutine(currentIndex);
    }
    [OnInspectorButton("適応コルーチンボタン",showOnlyInPlayMode = true)]
    private void DebugApply(int fromIndex,int toIndex,float changeTime = 5f)
    {
        if(fromIndex < 0 || fromIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("fromIndex is out of range");
            return;
        }
        if(toIndex < 0 || toIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("toIndex is out of range");
            return;
        }
        StartCoroutine(ApplyColorCorutine(fromIndex, toIndex, changeTime));
    }
    [OnInspectorButton("適応コルーチンボタン", showOnlyInPlayMode = true)]
    private void DebugApply(SkyBoxColorSetting fromIndex, SkyBoxColorSetting toIndex, float changeTime = 5f)
    {
        if(fromIndex == null || toIndex == null)
        {
            Debug.LogWarning("fromIndex or toIndex is null");
            return;
        }
        StartCoroutine(ApplyColorCorutine(fromIndex, toIndex, changeTime));
    }

    private void ApplyWithCorutine(int index)
    {
        if(index == 0)
        {
            StartCoroutine(ApplyColorCorutine(skyBoxColorSettings.Length -1, index, changeTime));
            return;
        }
        if(index < 1 || index >= skyBoxColorSettings.Length) return;
        StartCoroutine(ApplyColorCorutine(index-1,index, changeTime));
    }
    IEnumerator ApplyColorCorutine(int fromIndex,int toIndex,float changeTime)
    {
        for(float timer = 0f; timer <= changeTime; timer += Time.deltaTime)
        {
            ApplyLerp(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], timer / changeTime);
            yield return null;
        }
        ApplyColor(toIndex);
    }
    IEnumerator ApplyColorCorutine(SkyBoxColorSetting fromIndex, SkyBoxColorSetting toIndex, float changeTime)
    {
        for (float timer = 0f; timer <= changeTime; timer += Time.deltaTime)
        {
            ApplyLerp(fromIndex, toIndex, timer / changeTime);
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
    void ApplyLerp(SkyBoxColorSetting a, SkyBoxColorSetting b, float t)
    {
        skyboxMat.SetColor("_Color1", Color.Lerp(a.topColor, b.topColor, t));
        skyboxMat.SetColor("_Color2", Color.Lerp(a.horizonColor, b.horizonColor, t));
        skyboxMat.SetColor("_Color3", Color.Lerp(a.bottomColor, b.bottomColor, t));

        skyboxMat.SetFloat("_Intensity", Mathf.Lerp(a.intensity, b.intensity, t));
        skyboxMat.SetFloat("_Exponent1", Mathf.Lerp(a.exponentTop, b.exponentTop, t));
        skyboxMat.SetFloat("_Exponent2", Mathf.Lerp(a.exponentBottom, b.exponentBottom, t));
    }
}
