using UnityEngine;

public class GameClearEffectManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] NGameManager gameManager; // Inspectorで入れる（無ければ自動取得）

    [Header("Debug")]
    [SerializeField] bool logStateChange = false;

    // 状態監視
    GameState lastState;
    bool initialized;

    // 「GameClear演出を一度だけ実行」用
    bool clearApplied;

    // --- ここから先は演出用の参照を増やしていく ---
    [Header("Effect Targets")]
    [SerializeField] GameObject moonQuad;        // 窓の月(Quad)
    [SerializeField] Light[] roomLights;         // 部屋ライト
    [SerializeField] float clearLightIntensity = 2.0f;

    // Sky / Ambient をいったん簡易に（必要なら差し替え）
    [Header("Sky/Ambient (optional)")]
    [SerializeField] bool useAmbient = true;
    [SerializeField] Color clearAmbientColor = new(0.75f, 0.8f, 0.9f, 1f);
    [SerializeField] float clearAmbientIntensity = 1.2f;

    // --- デフォルト復元用 ---
    bool defaultsCached;
    bool defaultMoonActive;
    float[] defaultLightIntensities;
    Color defaultAmbientColor;
    float defaultAmbientIntensity;

    void Awake()
    {
        CacheDefaults();
    }

    void Start()
    {
        ResolveRefs();
        InitState();
    }

    void Update()
    {
        if (gameManager == null) return;

        var current = gameManager.CurrentGameState;

        // 初回
        if (!initialized)
        {
            lastState = current;
            initialized = true;
            return;
        }

        // 変化検知
        if (current != lastState)
        {
            if (logStateChange) Debug.Log($"[GameClearEffectManager] State: {lastState} -> {current}");
            OnStateChanged(lastState, current);
            lastState = current;
        }

        // 変化検知を使わず「常に監視したい」場合はここに追記も可能
        // 例：Playing中は徐々に暗くする…など
    }

    void ResolveRefs()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<NGameManager>();
    }

    void InitState()
    {
        if (gameManager == null) return;
        lastState = gameManager.CurrentGameState;
        initialized = true;
    }

    void OnStateChanged(GameState oldState, GameState newState)
    {
        // ゲームクリアに入った瞬間
        if (newState == GameState.GameClear)
        {
            ApplyGameClearOnce();
            return;
        }

        // 初期化/プレイに戻ったら演出を戻す（リトライ想定）
        if (newState == GameState.Initializing || newState == GameState.Playing)
        {
            RestoreDefaults();
        }

        // GameOver用演出を足したくなったらここに
        // if (newState == GameState.GameOver) ApplyGameOverOnce();
    }

    void ApplyGameClearOnce()
    {
        if (clearApplied) return;
        clearApplied = true;

        CacheDefaults();

        // ===== ここに「GameClear時にしたいこと」をどんどん足していく =====

        // 1) 窓の月(Quad)を消す
        if (moonQuad != null) moonQuad.SetActive(false);

        // 2) 部屋の光を強くする
        if (roomLights != null)
        {
            foreach (var l in roomLights)
                if (l != null) l.intensity = clearLightIntensity;
        }

        // 3) Sky / Ambient を明るくする（簡易版）
        if (useAmbient)
        {
            RenderSettings.ambientLight = clearAmbientColor;
            RenderSettings.ambientIntensity = clearAmbientIntensity;
        }

        // 例：BGM切り替え、ポストプロセス、UI表示、フェード等もここに追加
        // ================================================================
    }

    void RestoreDefaults()
    {
        // 「次のプレイのために戻す」
        clearApplied = false;

        CacheDefaults();

        if (moonQuad != null) moonQuad.SetActive(defaultMoonActive);

        if (roomLights != null && defaultLightIntensities != null)
        {
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) roomLights[i].intensity = defaultLightIntensities[i];
        }

        if (useAmbient)
        {
            RenderSettings.ambientLight = defaultAmbientColor;
            RenderSettings.ambientIntensity = defaultAmbientIntensity;
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

        defaultAmbientColor = RenderSettings.ambientLight;
        defaultAmbientIntensity = RenderSettings.ambientIntensity;
    }
}