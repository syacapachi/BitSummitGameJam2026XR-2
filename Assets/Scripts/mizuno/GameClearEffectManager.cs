using System.Collections;
using UnityEngine;

public class GameClearEffectManager : MonoBehaviour
{
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateRpcEvent;

    [Header("Debug")]
    [SerializeField] bool logStateChange = false;
    GameState lastState = GameState.Initializing;

    // State監視
    bool clearApplied;

    // ========== GameClearでやりたいことを詰めていく ==========
    [Header("Effect Targets")]
    [SerializeField] GameObject moonQuad;
    [SerializeField] Light[] roomLights;
    [SerializeField] float clearLightIntensity = 2.0f;
    [SerializeField] float lightFadeDuration = 2.0f;

    [Header("Sky Option A: Camera Solid Color")]
    [SerializeField] bool useCameraSolidColor = false;
    [SerializeField] Camera[] targetCameras;
    [SerializeField] Color clearBackgroundColor = new Color(0.70f, 0.80f, 1.00f, 1f);
    [SerializeField] bool forceSolidColorClearFlags = true;

    [Header("Sky Option B: RenderSettings Skybox")]
    [SerializeField] bool useRenderSettingsSkybox = true;
    [SerializeField] float clearSkyboxExposure = 1.3f;
    [SerializeField] Color clearSkyboxTint = Color.white;
    [SerializeField] float skyboxFadeDuration = 2.0f;

    [Header("Confetti Effect")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    [SerializeField] GameObject confettiPrefab;
    [SerializeField] Vector3 confettiPosition = new Vector3(0f, 3f, 0f);
    [SerializeField] float confettiLifeTime = 5f;
    [SerializeField] float confettiInterval = 0.8f;
    [SerializeField] int confettiCount = 3;

    [Header("Clear Sound")]
    [SerializeField] AudioClip clearSoundClip;
    [SerializeField, Range(0f, 1f)] float clearSoundVolume = 1.0f;
    [SerializeField] float clearSoundPitch = 1.0f;

    // --- デフォルト保持（リトライ用に戻す） ---
    bool defaultsCached;
    bool defaultMoonActive;
    float[] defaultLightIntensities;

    Color[] defaultCamBg;
    CameraClearFlags[] defaultCamFlags;

    Material originalSkyboxMat;
    Material runtimeSkyboxMat;

    Coroutine lightFadeCoroutine;
    Coroutine skyboxFadeCoroutine;
    Coroutine confettiCoroutine;

    void Start()
    {
        CacheDefaults();
    }

    private void OnEnable()
    {
        gameStateRpcEvent.Register(OnStateChanged);
    }

    private void OnDisable()
    {
        gameStateRpcEvent.Unregister(OnStateChanged);
    }

    void OnStateChanged(GameState newState)
    {
        if (logStateChange)
        {
            Debug.Log($"[GameClearEffectManager] {lastState} -> {newState}");
            lastState = newState;
        }

        if (newState == GameState.GameClear)
        {
            ApplyGameClearOnce();
            return;
        }

        // リトライで戻す想定
        if (newState == GameState.Initializing || newState == GameState.Playing)
        {
            RestoreDefaults();
        }
    }

    void ApplyGameClearOnce()
    {
        if (clearApplied) return;
        clearApplied = true;

        CacheDefaults();

        // 1) 月Quadを消す
        if (moonQuad != null) moonQuad.SetActive(false);

        // 2) 部屋ライトをじわっと明るくする
        if (roomLights != null)
        {
            if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);
            lightFadeCoroutine = StartCoroutine(FadeLights(clearLightIntensity, lightFadeDuration));
        }

        // 3-A) 背景色で空を明るくする（Solid Color用）
        if (useCameraSolidColor && targetCameras != null)
        {
            for (int i = 0; i < targetCameras.Length; i++)
            {
                var cam = targetCameras[i];
                if (cam == null) continue;

                if (forceSolidColorClearFlags)
                    cam.clearFlags = CameraClearFlags.SolidColor;

                cam.backgroundColor = clearBackgroundColor;
            }
        }

        // 3-B) Skyboxをじわっと明るくする（Skybox用）
        if (useRenderSettingsSkybox && runtimeSkyboxMat != null)
        {
            if (skyboxFadeCoroutine != null) StopCoroutine(skyboxFadeCoroutine);
            skyboxFadeCoroutine = StartCoroutine(FadeSkybox(clearSkyboxExposure, clearSkyboxTint, skyboxFadeDuration));
        }

        // 4) クリアSEを鳴らす
        if (gameEffectEvent != null && clearSoundClip != null)
        {
            var audioEffect = new AudioEffect(clearSoundClip, clearSoundVolume, clearSoundPitch);
            gameEffectEvent.Invoke(new GameEffect(audioEffect, confettiPosition));
        }

        // 5) 紙吹雪を複数回に分けて発射する
        if (gameEffectEvent != null && confettiPrefab != null)
        {
            if (confettiCoroutine != null) StopCoroutine(confettiCoroutine);
            confettiCoroutine = StartCoroutine(PlayConfettiSequence());
        }
    }

    // ライトをじわっと明るくするコルーチン
    IEnumerator FadeLights(float targetIntensity, float duration)
    {
        float elapsed = 0f;

        float[] startIntensities = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            startIntensities[i] = roomLights[i] ? roomLights[i].intensity : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] != null)
                    roomLights[i].intensity = Mathf.Lerp(startIntensities[i], targetIntensity, t);
            }

            yield return null;
        }

        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null)
                roomLights[i].intensity = targetIntensity;
    }

    // スカイボックスをじわっと明るくするコルーチン
    IEnumerator FadeSkybox(float targetExposure, Color targetTint, float duration)
    {
        if (runtimeSkyboxMat == null) yield break;

        float elapsed = 0f;

        float startExposure = runtimeSkyboxMat.HasProperty("_Exposure")
            ? runtimeSkyboxMat.GetFloat("_Exposure") : 1f;
        Color startTint = runtimeSkyboxMat.HasProperty("_Tint")
            ? runtimeSkyboxMat.GetColor("_Tint") : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float currentExposure = Mathf.Lerp(startExposure, targetExposure, t);
            Color currentTint = Color.Lerp(startTint, targetTint, t);

            SetSkyboxBrightness(runtimeSkyboxMat, currentExposure, currentTint);

            yield return null;
        }

        SetSkyboxBrightness(runtimeSkyboxMat, targetExposure, targetTint);
    }

    // 紙吹雪を間隔をあけて複数回発射するコルーチン
    IEnumerator PlayConfettiSequence()
    {
        for (int i = 0; i < confettiCount; i++)
        {
            var fxEffect = new FxEffect(confettiPrefab, confettiLifeTime);
            gameEffectEvent.Invoke(new GameEffect(fxEffect, confettiPosition));

            yield return new WaitForSeconds(confettiInterval);
        }
    }

    void RestoreDefaults()
    {
        clearApplied = false;

        if (lightFadeCoroutine != null) { StopCoroutine(lightFadeCoroutine); lightFadeCoroutine = null; }
        if (skyboxFadeCoroutine != null) { StopCoroutine(skyboxFadeCoroutine); skyboxFadeCoroutine = null; }
        if (confettiCoroutine != null) { StopCoroutine(confettiCoroutine); confettiCoroutine = null; }

        CacheDefaults();

        if (moonQuad != null) moonQuad.SetActive(defaultMoonActive);

        if (roomLights != null && defaultLightIntensities != null)
        {
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) roomLights[i].intensity = defaultLightIntensities[i];
        }

        // Camera restore
        if (useCameraSolidColor && targetCameras != null && defaultCamBg != null && defaultCamFlags != null)
        {
            for (int i = 0; i < targetCameras.Length; i++)
            {
                var cam = targetCameras[i];
                if (cam == null) continue;
                cam.clearFlags = defaultCamFlags[i];
                cam.backgroundColor = defaultCamBg[i];
            }
        }

        // Skybox restore
        if (useRenderSettingsSkybox)
        {
            if (originalSkyboxMat != null)
                RenderSettings.skybox = originalSkyboxMat;
        }
    }

    void CacheDefaults()
    {
        if (defaultsCached) return;
        defaultsCached = true;

        if (moonQuad != null) defaultMoonActive = moonQuad.activeSelf;

        if (roomLights != null)
        {
            defaultLightIntensities = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
                defaultLightIntensities[i] = roomLights[i] ? roomLights[i].intensity : 1f;
        }

        // Camera defaults
        if (targetCameras != null)
        {
            defaultCamBg = new Color[targetCameras.Length];
            defaultCamFlags = new CameraClearFlags[targetCameras.Length];
            for (int i = 0; i < targetCameras.Length; i++)
            {
                var cam = targetCameras[i];
                defaultCamBg[i] = cam ? cam.backgroundColor : Color.black;
                defaultCamFlags[i] = cam ? cam.clearFlags : CameraClearFlags.Skybox;
            }
        }

        // Skybox defaults（アセットを直接いじらないよう、ランタイム複製を使う）
        originalSkyboxMat = RenderSettings.skybox;
        if (originalSkyboxMat != null)
        {
            runtimeSkyboxMat = new Material(originalSkyboxMat);
            RenderSettings.skybox = runtimeSkyboxMat;
        }
    }

    static void SetSkyboxBrightness(Material sky, float exposure, Color tint)
    {
        if (sky == null) return;

        if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", exposure);
        if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", tint);
        if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", tint);
        if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", tint);
    }
}
