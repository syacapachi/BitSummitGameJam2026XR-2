using Syacapachi.Attribute;
using System.Collections;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [SerializeField] DifficultyDataBase rpcDataBase;
    [Header("重増し時間")]
    [SerializeField] float timeOffset = 30f;
    [SerializeField] float clearChangeTime = 5f;
    [SerializeField] float tutorialToEveningTime = 3f;
    [Header("SkyBox Material")]
    [SerializeField] Material[] skyboxMats;
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
    private GameState previousState;
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
            case GameState.Home:
                ApplyColor(noonSky); break;
            case GameState.Tutorial:
                StartCoroutine(ApplyColorCorutine(currentSky, startSky, tutorialToEveningTime));break;

            case GameState.Playing:
                if (previousState != GameState.Tutorial)
                {
                    StartCoroutine(TutorialToGameCoroutine());
                }
                else
                {
                    StartSkyBoxChange();
                }
                break;

            case GameState.GameClear:
                StopAllCoroutines();
                StartCoroutine(GameClearSkyCoroutine()); break;

            case GameState.GameOver:
                StopAllCoroutines();
                StartCoroutine(ApplyColorCorutine(currentSky, gameOverSky, clearChangeTime));
                break;
        }
        previousState = newState;
    }
    private void StartSkyBoxChange()
    {
        float maxPhaseTime = timeOffset;
        foreach (var phase in rpcDataBase.CurrentSetting.Phases)
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

    IEnumerator TutorialToGameCoroutine()
    {
        // noon → evening を急速変化
        yield return ApplyColorCorutine(
            currentSky,
            startSky,
            tutorialToEveningTime);

        //その後、いつもの流れ
        StartSkyBoxChange();
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
        currentSky.CopySky(setting);
        foreach (var skyboxMat in skyboxMats)
        {
            skyboxMat.SetColor("_Color1", setting.topColor);
            skyboxMat.SetColor("_Color2", setting.horizonColor);
            skyboxMat.SetColor("_Color3", setting.bottomColor);
            skyboxMat.SetFloat("_Intensity", setting.intensity);
            skyboxMat.SetFloat("_Exponent1", setting.exponentTop);
            skyboxMat.SetFloat("_Exponent2", setting.exponentBottom);
            if (skyboxMat.HasFloat("_TextureStrength"))
            {
                skyboxMat.SetFloat("_TextureStrength", setting.textureStrength);
                skyboxMat.SetFloat("_Rotate", setting.textureRotation);
            }
        }


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
        //今の状態を更新する
        currentSky.UpdateLerpSky(a, b, t);
        foreach (var skyboxMat in skyboxMats)
        {
            skyboxMat.SetColor("_Color1", currentSky.topColor);
            skyboxMat.SetColor("_Color2", currentSky.horizonColor);
            skyboxMat.SetColor("_Color3", currentSky.bottomColor);

            skyboxMat.SetFloat("_Intensity", currentSky.intensity);
            skyboxMat.SetFloat("_Exponent1", currentSky.exponentTop);
            skyboxMat.SetFloat("_Exponent2", currentSky.exponentBottom);
            if (skyboxMat.HasFloat("_TextureStrength"))
            {
                skyboxMat.SetFloat("_TextureStrength", currentSky.textureStrength);
                skyboxMat.SetFloat("_Rotate",currentSky.textureRotation);
            }
        }
        sunLight.color = currentSky.sunColor;
        sunLight.intensity = currentSky.intensity;
        sunLight.bounceIntensity = currentSky.sunMultiplier;
        sunMat.SetColor("Emission", currentSky.sunColor);

        moonLight.color = currentSky.moonColor;
        moonLight.intensity = currentSky.moonIntensity;
        moonLight.bounceIntensity = currentSky.moonMultiplier;
        moonMat.SetColor("Emission", currentSky.moonColor);

        sunAndMoonRoot.rotation = currentSky.SkyRotation;
    }
}
