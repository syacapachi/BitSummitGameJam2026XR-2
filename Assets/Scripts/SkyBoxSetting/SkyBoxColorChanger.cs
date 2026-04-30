using Syacapachi.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [SerializeField] float changeTime = 5f;
    [Header("SkyBox Material")]
    [SerializeField] Material skyboxMat;
    [SerializeField] SkyBoxColorSetting[] skyBoxColorSettings;
    [SerializeField] int defaultIndex = 0;
    [Header("Directional Light(太陽)")]
    [SerializeField] Light[] directionalLights;
    [Header("Subscribe Event")]
    [SerializeField] IntEvent phaseChageEvent;
    [Header("デバックモード")]
    [SerializeField] bool IsDebug;
    [SerializeField, EnableIf(nameof(IsDebug))] VoidEvent debugEvent;
    private readonly Dictionary<TimeOfDay,SkyBoxColorSetting> settingDic = new();
    private bool isInitialized = false;
    private int currentIndex = 0;
    private void Awake()
    {
        SettingDic();
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
    void SettingDic()
    {
        if(isInitialized) return;
        isInitialized = true;
        settingDic.Clear();
        foreach(var setting in skyBoxColorSettings)
        {
            if(settingDic.ContainsKey(setting.timeOfDay))
            {
                Debug.LogWarning($"同じTimeOfDayの設定が複数あります。timeOfDay:{setting.timeOfDay}");
                continue;
            }
            settingDic.Add(setting.timeOfDay, setting);
        }
    }
    /// <summary>
    /// 即時変更用。
    /// </summary>
    /// <param name="index"></param>
    void ApplyColor(int index)
    {
        if (index < 0 || index >= skyBoxColorSettings.Length) return;
        ApplyColor(skyBoxColorSettings[index]);
    }
    [OnInspectorButton("適応ボタン(設定)", validateInvoke = true)]
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

        foreach(Light light in directionalLights)
        {
            light.color = setting.lightColor;
            light.intensity = setting.lightIntensity;
            light.bounceIntensity = setting.indirectMultiplier;
            light.transform.rotation = Quaternion.Euler(setting.lightRotation);
        }
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
        DebugApply(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], changeTime);
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
            DebugApply(skyBoxColorSettings[skyBoxColorSettings.Length -1], skyBoxColorSettings[index], changeTime);
            return;
        }
        if(index < 1 || index >= skyBoxColorSettings.Length) return;
        DebugApply(skyBoxColorSettings[index-1], skyBoxColorSettings[index], changeTime);
    }
    IEnumerator ApplyColorCorutine(int fromIndex,int toIndex,float changeTime)
    {
        if(fromIndex < 0 || fromIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("fromIndex is out of range");
            yield break;
        }
        yield return ApplyColorCorutine(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], changeTime);
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

        foreach(Light light in directionalLights)
        {
            light.color = Color.Lerp(a.lightColor, b.lightColor, t);
            light.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t);
            light.bounceIntensity = Mathf.Lerp(a.indirectMultiplier, b.indirectMultiplier, t);
            light.transform.rotation = Quaternion.Lerp(Quaternion.Euler(a.lightRotation), Quaternion.Euler(b.lightRotation), t);
        }
    }
}
