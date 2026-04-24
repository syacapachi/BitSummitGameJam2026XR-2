using System.Collections;
using UnityEngine;

public class GameClearEffectManager : MonoBehaviour
{
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateRpcEvent;
    [SerializeField] IntEvent phaseChangeEvent;

    [Header("Debug")]
    [SerializeField] bool logStateChange = false;
    GameState lastState = GameState.Initializing;

    // State監視
    bool clearApplied;

    // ========== ここから「GameClearでやりたいこと」を詰めていく ==========
    [Header("Effect Targets")]
    [SerializeField] GameObject moonQuad;        // 窓の月(Quad)を消す
    [SerializeField] Light[] roomLights;         // 部屋の光を強くする
    [SerializeField] float clearLightIntensity = 2.0f;

    [Header("Sky Option A: Camera Solid Color")]
    [SerializeField] bool useCameraSolidColor = false;
    [SerializeField] Camera[] targetCameras;     // 外が「背景色」ならここにMainCamera等を入れる
    [SerializeField] Color clearBackgroundColor = new Color(0.70f, 0.80f, 1.00f, 1f);
    [SerializeField] bool forceSolidColorClearFlags = true; // SkyboxならSolidColorに変える

    [Header("Sky Option B: RenderSettings Skybox")]
    [SerializeField] bool useRenderSettingsSkybox = true;
    [SerializeField] float clearSkyboxExposure = 1.3f;      // 明るさ
    [SerializeField] Color clearSkyboxTint = Color.white;   // 色味（白でOK）
    // ===============================================================

    // --- デフォルト保持（リトライ用に戻す） ---
    bool defaultsCached;
    bool defaultMoonActive;
    float[] defaultLightIntensities;

    // Camera defaults
    Color[] defaultCamBg;
    CameraClearFlags[] defaultCamFlags;

    // Skybox defaults
    Material originalSkyboxMat;
    Material runtimeSkyboxMat;

    void Start()
    {
        CacheDefaults();
    }
    private void OnEnable()
    {
        gameStateRpcEvent.Register(OnStateChanged);
        phaseChangeEvent.Register(OnPhaseChanged);
    }
    private void OnDisable()
    {
        gameStateRpcEvent.Unregister(OnStateChanged);
        phaseChangeEvent.Unregister(OnPhaseChanged);
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
    void OnPhaseChanged(int phaseIndex)
    {

    }

    void ApplyGameClearOnce()
    {
        if (clearApplied) return;
        clearApplied = true;

        CacheDefaults();

        // 1) 月Quadを消す
        if (moonQuad != null) moonQuad.SetActive(false);

        // 2) 部屋ライトを強くする
        if (roomLights != null)
        {
            foreach (var l in roomLights)
                if (l != null) l.intensity = clearLightIntensity;
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

        // 3-B) Skybox（RenderSettings）を明るくする（Skybox用）
        if (useRenderSettingsSkybox && runtimeSkyboxMat != null)
        {
            SetSkyboxBrightness(runtimeSkyboxMat, clearSkyboxExposure, clearSkyboxTint);
        }
    }

    void RestoreDefaults()
    {
        clearApplied = false;
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
            // 元のマテリアルに戻す
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
        // Skyboxシェーダによってプロパティ名が違うので「存在チェックして書く」
        if (sky == null) return;

        // Procedural Skybox でよくある
        if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", exposure);
        if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", tint);

        // たまにある別名
        if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", tint);
        if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", tint);
    }
}