using Syacapachi.Attribute;
using System;
using System.Collections;
using UnityEngine;

public class SkyBoxColorChanger : MonoBehaviour
{
    [SerializeField] DifficultyDataBase rpcDataBase;
    [Header("重増し時間")]
    [SerializeField] float timeOffset = 30f;
    [SerializeField] float clearChangeTime = 5f;
    [SerializeField] float tutorialToEveningTime = 3f;
    [SerializeField] float oneHourTime = 10f;
    [Header("空を回し始める時間")]
    [SerializeField] bool useDayLoop = true;
    [SerializeField] float startDayLoopTime = 30f;
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
    [Header("空の設定")]
    [SerializeField] SkyBoxColorSetting[] skyBoxColorSettings;
    [SerializeField] SkyBoxColorSetting[] dayLoopSkyColorSettings;
    [SerializeField] SkyBoxColorSetting startSky;
    [SerializeField] SkyBoxColorSetting clearSky;
    [SerializeField] SkyBoxColorSetting noonSky;
    [SerializeField] SkyBoxColorSetting gameOverSky;
    [Header("書き込み専用")]
    [SerializeField] SkyBoxColorSetting currentSky;
    [SerializeField] SkyBoxColorSetting copySky;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    [SerializeField] LocalStateEvent localStateEvent;
    [Header("デバックモード")]
    [SerializeField] bool IsDebug;
    [SerializeField, EnableIf(nameof(IsDebug))] VoidEvent debugEvent;

    private int currentIndex = 0;
    private GameState previousState = GameState.Home;
    /// <summary>
    /// 一日の状態変化を行うコルーチン
    /// </summary>
    private Coroutine dayLoopCoroutine;


    private void Awake()
    {
        ApplyColor(noonSky);
        OnLocalStateChanged(LocalState.LanguageSelect);
    }
    private void OnEnable()
    {
        gameStateEvent.Register(OnGameStateChanged);
        localStateEvent.Register(OnLocalStateChanged);
        if (IsDebug)
        {
            debugEvent.Register(DebugApply);
        }
    }
    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChanged);
        localStateEvent.Unregister(OnLocalStateChanged);
        if (IsDebug)
        {
            debugEvent.Unregister(DebugApply);
        }
    }
    /// <summary>
    /// ゲーム状態が変化したときに呼ばれるイベント受信関数。
    /// </summary>
    /// <param name="newState"> 次の状態 </param>
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Initializing:
            case GameState.Home:
                StopAllCoroutines();
                ApplyColor(noonSky); break;

            case GameState.Tutorial:
                if (dayLoopCoroutine != null)
                {
                    StopCoroutine(dayLoopCoroutine);
                }
                copySky.CopySky(currentSky);
                StartCoroutine(ApplyColorCorutineByTime(copySky, startSky, tutorialToEveningTime)); break;

            case GameState.Playing:
                if (dayLoopCoroutine != null)
                {
                    StopCoroutine(dayLoopCoroutine);
                }
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
                copySky.CopySky(currentSky);
                StartCoroutine(ApplyColorCorutineByTime(copySky, gameOverSky, clearChangeTime));
                break;
        }
        previousState = newState;
    }
    /// <summary>
    /// UI状態が変化したら呼ばれる
    /// </summary>
    /// <param name="newLocalState"> 次のローカル状態 </param>
    private void OnLocalStateChanged(LocalState newLocalState)
    {
        if (!useDayLoop) return;
        if(dayLoopCoroutine != null)
        {
            StopCoroutine(dayLoopCoroutine);
        }
        dayLoopCoroutine = StartCoroutine(DayLoopSkyCoroutine());
    }
    private void StartSkyBoxChange()
    {
        float maxPhaseTime = timeOffset;
        foreach (var phase in rpcDataBase.CurrentSetting.Phases)
        {
            maxPhaseTime += phase.PhaseTime;
        }
        StartCoroutine(SkyBoxChangeCorutine(skyBoxColorSettings, maxPhaseTime));
    }
    private IEnumerator SkyBoxChangeCorutine(SkyBoxColorSetting[] settings,float maxTime)
    {
        int startTime = (int)settings[0].timeOfDay;
        int endTime = (int)settings[^1].timeOfDay;
        int duratinTime = endTime - startTime > 0
            ? endTime - startTime
            : (24 - startTime) + (endTime);

        float hourTime = maxTime / duratinTime;

        for (int index = 0; index < settings.Length; index++)
        {
            if (index == settings.Length - 1) yield return ApplyColorCorutineByHour(settings[^1], clearSky, hourTime);
            else yield return ApplyColorCorutineByHour(settings[index], settings[index + 1], hourTime);
        }
    }

    IEnumerator TutorialToGameCoroutine()
    {
        copySky.CopySky(currentSky);
        // noon → evening を急速変化
        yield return ApplyColorCorutineByTime(
            copySky,
            startSky,
            tutorialToEveningTime);

        //その後、いつもの流れ
        StartSkyBoxChange();
    }
    private IEnumerator GameClearSkyCoroutine()
    {
        copySky.CopySky(currentSky);
        yield return ApplyColorCorutineByTime(copySky, clearSky, clearChangeTime);
        yield return ApplyColorCorutineByHour(clearSky, noonSky, oneHourTime);
    }
    private IEnumerator DayLoopSkyCoroutine()
    {
        ApplyColor(noonSky);
        float timer = 0;
        while(timer < startDayLoopTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        while (previousState == GameState.Home && useDayLoop)
        {
            foreach (var nextSky in dayLoopSkyColorSettings)
            {
                copySky.CopySky(currentSky);
                if (copySky.timeOfDay == nextSky.timeOfDay) continue;
                yield return ApplyColorCorutineByHour(copySky, nextSky, oneHourTime);
                ApplyColor(nextSky);
            }
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
    [OnInspectorButton("適応ボタン(設定)", ValidateInvoke = true)]
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
        if (index == 0)
        {
            StartCoroutine(ApplyColorCorutineByHour(skyBoxColorSettings[^1], skyBoxColorSettings[index], clearChangeTime));
            return;
        }
        if (index < 1 || index >= skyBoxColorSettings.Length) return;
        StartCoroutine(ApplyColorCorutineByHour(skyBoxColorSettings[index - 1], skyBoxColorSettings[index], clearChangeTime));
    }
    IEnumerator ApplyColorCorutineByHour(int fromIndex, int toIndex, float hourTime)
    {
        if (fromIndex < 0 || fromIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("from is out of range", gameObject);
            yield break;
        }
        yield return ApplyColorCorutineByHour(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], hourTime);
    }
    IEnumerator ApplyColorCorutineByHour(SkyBoxColorSetting from, SkyBoxColorSetting to, float hourTime)
    {
        int hours = to.timeOfDay - from.timeOfDay;
        if (hours < 0)
        {
            hours += 24;
        }
        float changeTime = hourTime * hours;
        yield return ApplyColorCorutineByTime(from, to, changeTime);
    }
    [OnInspectorButton(showOnlyInPlayMode: true)]
    void DebugSkyChange(SkyBoxColorSetting from, SkyBoxColorSetting to, float changeTime)
    {
        StartCoroutine(ApplyColorCorutineByTime(from, to, changeTime));
    }
    IEnumerator ApplyColorCorutineByTime(int fromIndex, int toIndex, float changeTime)
    {
        if (fromIndex < 0 || fromIndex >= skyBoxColorSettings.Length)
        {
            Debug.LogWarning("from is out of range", gameObject);
            yield break;
        }
        yield return ApplyColorCorutineByTime(skyBoxColorSettings[fromIndex], skyBoxColorSettings[toIndex], changeTime);
    }
    /// <summary>
    /// From設定からTo設定をChangeTimeで遷移。
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="changeTime"> not 0 and negative</param>
    /// <returns></returns>
    IEnumerator ApplyColorCorutineByTime(SkyBoxColorSetting from, SkyBoxColorSetting to, float changeTime)
    {
        float timer = 0f;
        if (changeTime <= 0f)
        {
            Debug.LogError("ChangeTime is not negative", gameObject);
            yield break;
        }
        float invChangeTime = 1f / changeTime;
        while (timer < changeTime)
        {
            timer += Time.deltaTime;

            float t = timer * invChangeTime;

            ApplyLerp(from, to, t);

            yield return null;
        }

        ApplyColor(to);
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
                skyboxMat.SetFloat("_Rotate", currentSky.textureRotation);
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