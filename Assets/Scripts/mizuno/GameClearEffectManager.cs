using System.Collections;
using UnityEngine;

public class GameClearEffectManager : MonoBehaviour
{
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateRpcEvent;

    [Header("Effect Targets")]
    [SerializeField] Light[] roomLights;
    [SerializeField] float clearLightIntensity = 2f;
    [SerializeField] float lightFadeDuration = 2f;

    [Header("Confetti Effect")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    [SerializeField] GameObject confettiPrefab;
    [SerializeField] Vector3 confettiPosition = new Vector3(0f, 3f, 0f);
    [SerializeField] float confettiLifeTime = 5f;
    [SerializeField] float confettiInterval = 0.8f;
    [SerializeField] int confettiCount = 3;

    private bool clearApplied;
    private float[] defaultLightIntensities;
    private Coroutine lightFadeCoroutine;
    private Coroutine confettiCoroutine;

    void Start() => CacheDefaults();

    void OnEnable() => gameStateRpcEvent.Register(OnStateChanged);
    void OnDisable() => gameStateRpcEvent.Unregister(OnStateChanged);

    void OnStateChanged(GameState newState)
    {
        if (newState == GameState.GameClear)
            ApplyGameClearOnce();
        else if (newState == GameState.Initializing || newState == GameState.Playing)
            RestoreDefaults();
    }

    void ApplyGameClearOnce()
    {
        if (clearApplied) return;
        clearApplied = true;

        // ライトをフェードアップ
        if (roomLights != null && roomLights.Length > 0)
        {
            if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);
            lightFadeCoroutine = StartCoroutine(FadeLights(clearLightIntensity, lightFadeDuration));
        }

        // 紙吹雪
        if (gameEffectEvent != null && confettiPrefab != null)
        {
            if (confettiCoroutine != null) StopCoroutine(confettiCoroutine);
            confettiCoroutine = StartCoroutine(PlayConfettiSequence());
        }
    }

    void RestoreDefaults()
    {
        clearApplied = false;
        if (lightFadeCoroutine != null) { StopCoroutine(lightFadeCoroutine); lightFadeCoroutine = null; }
        if (confettiCoroutine != null) { StopCoroutine(confettiCoroutine); confettiCoroutine = null; }
        if (roomLights != null)
        {
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i]) roomLights[i].intensity = defaultLightIntensities[i];
        }
    }

    void CacheDefaults()
    {
        defaultLightIntensities = new float[roomLights != null ? roomLights.Length : 0];
        for (int i = 0; i < defaultLightIntensities.Length; i++)
            defaultLightIntensities[i] = roomLights[i] ? roomLights[i].intensity : 1f;
    }

    IEnumerator FadeLights(float target, float duration)
    {
        float[] start = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            start[i] = roomLights[i] ? roomLights[i].intensity : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i]) roomLights[i].intensity = Mathf.Lerp(start[i], target, t);
            yield return null;
        }
        foreach (var l in roomLights)
            if (l) l.intensity = target;
    }

    IEnumerator PlayConfettiSequence()
    {
        for (int i = 0; i < confettiCount; i++)
        {
            var fx = new FxEffect(confettiPrefab, confettiLifeTime);
            gameEffectEvent.Invoke(new GameEffect(fx, confettiPosition));
            yield return new WaitForSeconds(confettiInterval);
        }
    }
}
