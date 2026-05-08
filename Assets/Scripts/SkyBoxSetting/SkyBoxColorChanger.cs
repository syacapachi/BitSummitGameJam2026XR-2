using Syacapachi.Attribute;
using System.Collections;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [SerializeField] PhaseManager phaseManager;
    [Header("重増し時間")]
    [SerializeField] float timeOffset = 30f;
    [SerializeField] float clearChangeTime = 5f;
    [Header("SkyBox Material")]
    [SerializeField] Material skyboxMat;
    [Header("太陽と月のルート")]
    [SerializeField] Transform sunAndMoonRoot;
    [Header("Directional Light(太陽)")]
    [SerializeField] Light sunLight;
    [SerializeField] Material sunMat;
    [Header("月")]
    [SerializeField] Light moonLight;
    [SerializeField] Material moonMat;
    [Header("時間の設定")]
    [SerializeField] SkyBoxColorSetting[] skyBoxColorSettings;
    [SerializeField] SkyBoxColorSetting startSky;
    [SerializeField] SkyBoxColorSetting clearSky;
    [SerializeField] SkyBoxColorSetting noonSky;
    [SerializeField] SkyBoxColorSetting gameOverSky;
    [Header("書き込み専用")]
    [SerializeField] SkyBoxColorSetting currentSky;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    [Header("デバックモード")]
    [SerializeField] bool IsDebug;
    [SerializeField, EnableIf(nameof(IsDebug))] VoidEvent debugEvent;

    private int currentIndex = 0;
    private void Awake()
    {
        ApplyColor(startSky);
    }
    private void OnEnable()
    {
        gameStateEvent.Register(OnGameStateChanged);
        if (IsDebug)
        {
            debugEvent.Register(DebugApply);
        }
    }
    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChanged);
        if (IsDebug)
        {
            debugEvent.Unregister(DebugApply);
        }
    }
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Initializing:
                ApplyColor(startSky); break;
            case GameState.Playing:
                StartSkyBoxChange(); break;
            case GameState.GameClear:
                StopAllCoroutines();
                StartCoroutine(GameClearSkyCoroutine()); break;
            case GameState.GameOver:
                StopAllCoroutines();
                StartCoroutine(ApplyColorCorutine(currentSky, gameOverSky, clearChangeTime));
                break;
        }
    }
    private void StartSkyBoxChange()
    {
        float maxPhaseTime = timeOffset;
        foreach (var phase in phaseManager.Phases)
        {
            maxPhaseTime += phase.PhaseTime;
        }
        StartCoroutine(SkyBoxChangeCorutine(maxPhaseTime));
    }
    private IEnumerator SkyBoxChangeCorutine(float maxTime)
    {
        int startTime = (int)skyBoxColorSettings[0].timeOfDay;
        int endTime = (int)skyBoxColorSettings[^1].timeOfDay;
        int duratinTime = endTime - startTime > 0 
            ? endTime - startTime
            :(24 - startTime) + (endTime);

        float hourTime = maxTime / duratinTime;

        for (int index = 0; index < skyBoxColorSettings.Length; index++)
        {
            if (index == skyBoxColorSettings.Length - 1) yield return ApplyColorCorutine(skyBoxColorSettings[^1], clearSky, hourTime);
            else yield return ApplyColorCorutine(index, index + 1, hourTime);
        }
    }
    private IEnumerator GameClearSkyCoroutine()
    {
        yield return ApplyColorCorutine(currentSky, clearSky, clearChangeTime);
        yield return ApplyColorCorutine(clearSky, noonSky, 20f);
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
            Debug.LogWarning("setting is null", gameObject);
            return;
        }
        skyboxMat.SetColor("_Color1", setting.topColor);
        skyboxMat.SetColor("_Color2", setting.horizonColor);
        skyboxMat.SetColor("_Color3", setting.bottomColor);
        skyboxMat.SetFloat("_Intensity", setting.intensity);
        skyboxMat.SetFloat("_Exponent1", setting.exponentTop);
        skyboxMat.SetFloat("_Exponent2", setting.exponentBottom);


        sunLight.color = setting.sunColor;
        sunLight.intensity = setting.sunIntensity;
        sunLight.bounceIntensity = setting.sunMultiplier;
        sunMat.SetColor("_EmissionColor", setting.sunColor);

        moonLight.color = setting.moonColor;
        moonLight.intensity = setting.moonIntensity;
        moonLight.bounceIntensity = setting.moonMultiplier;
        moonMat.SetColor("_EmissionColor", setting.moonColor);

        sunAndMoonRoot.rotation = setting.SkyRotation;
    }
    private void DebugApply()
    {
        currentIndex = (currentIndex + 1) % skyBoxColorSettings.Length;
        ApplyWithCorutine(currentIndex);
    }
    private void ApplyWithCorutine(int index)
    {
        if(index == 0)
        {
            StartCoroutine(ApplyColorCorutine(skyBoxColorSettings[^1], skyBoxColorSettings[index], clearChangeTime));
            return;
        }
        if(index < 1 || index >= skyBoxColorSettings.Length) return;
        StartCoroutine(ApplyColorCorutine(skyBoxColorSettings[index-1], skyBoxColorSettings[index], clearChangeTime));
    }
    IEnumerator ApplyColorCorutine(int fromIndex,int toIndex,float hourTime)
    {
        if(fromIndex < 0 || fromIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("fromIndex is out of range",gameObject);
            yield break;
        }
        yield return ApplyColorCorutine(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], hourTime);
    }
    IEnumerator ApplyColorCorutine(SkyBoxColorSetting fromIndex, SkyBoxColorSetting toIndex, float hourTime)
    {
        int hours = toIndex.timeOfDay - fromIndex.timeOfDay;
        if(hours < 0)
        {
            hours += 24;
        }
        float changeTime = hourTime * hours;
        for (float timer = 0f; timer <= changeTime; timer += Time.deltaTime)
        {
            ApplyLerp(fromIndex, toIndex, timer / changeTime);
            yield return null;
        }
        ApplyColor(toIndex);
    }
    [OnInspectorButton(showOnlyInPlayMode: true)]
    void DebugSkyChange(SkyBoxColorSetting fromIndex, SkyBoxColorSetting toIndex, float changeTime)
    {
        StartCoroutine(ApplyColorCorutineByTime(fromIndex, toIndex, changeTime));
    }
    IEnumerator ApplyColorCorutineByTime(SkyBoxColorSetting fromIndex, SkyBoxColorSetting toIndex, float changeTime)
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
        currentSky.UpdateLerpSky(a, b, t);
        skyboxMat.SetColor("_Color1", Color.Lerp(a.topColor, b.topColor, t));
        skyboxMat.SetColor("_Color2", Color.Lerp(a.horizonColor, b.horizonColor, t));
        skyboxMat.SetColor("_Color3", Color.Lerp(a.bottomColor, b.bottomColor, t));

        skyboxMat.SetFloat("_Intensity", Mathf.Lerp(a.intensity, b.intensity, t));
        skyboxMat.SetFloat("_Exponent1", Mathf.Lerp(a.exponentTop, b.exponentTop, t));
        skyboxMat.SetFloat("_Exponent2", Mathf.Lerp(a.exponentBottom, b.exponentBottom, t));

        sunLight.color = Color.Lerp(a.sunColor, b.sunColor, t);
        sunLight.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);
        sunLight.bounceIntensity = Mathf.Lerp(a.sunMultiplier, b.sunMultiplier, t);
        sunMat.SetColor("Emission", Color.Lerp(a.sunColor, b.sunColor, t));

        moonLight.color = Color.Lerp(a.moonColor, b.moonColor, t);
        moonLight.intensity = Mathf.Lerp(a.moonIntensity, b.moonIntensity, t);
        moonLight.bounceIntensity = Mathf.Lerp(a.moonMultiplier, b.moonMultiplier, t);
        moonMat.SetColor("Emission", Color.Lerp(a.moonColor, b.moonColor, t));

        sunAndMoonRoot.rotation = Quaternion.Lerp(a.SkyRotation, b.SkyRotation, t);
    }
}
