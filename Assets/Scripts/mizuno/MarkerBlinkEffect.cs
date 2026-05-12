using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MarkerBlinkEffect : NetworkBehaviour
{
    [Header("Color Blink")]
    [SerializeField] Renderer[] targetRenderers;
    [SerializeField] Color ownerNormalColor = Color.green;
    [SerializeField] Color nonOwnerNormalColor = Color.blue;
    [SerializeField] Color blinkColor = Color.red;
    [SerializeField] float blinkSpeed = 4.5f;
    [SerializeField, Range(0f, 1f)] float blinkSaturationScale = 0.6f;
    [SerializeField, Range(0f, 1f)] float blinkAlpha = 0.72f;
    [SerializeField] bool useEmission = true;
    [SerializeField] float emissionIntensity = 3.2f;

    [Header("Scale Pulse")]
    [SerializeField] Transform scaleTarget;
    [SerializeField] float scaleSpeed = 2.2f;
    [SerializeField] float scaleMultiplier = 1.08f;
    Color DefaultColor;
    Material[] materials;
    bool isBlinking = false;
    Vector3 defaultScale;
    Coroutine blinkCoroutine;

    public override void OnNetworkSpawn()
    {
        DefaultColor = IsOwner ? ownerNormalColor : nonOwnerNormalColor;
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        if (scaleTarget == null)
            scaleTarget = transform;

        defaultScale = scaleTarget.localScale;

        materials = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            materials[i] = targetRenderers[i].material;
            ApplyColor(materials[i], DefaultColor);
        }
    }

    private void Update()
    {
        if (!isBlinking) return;

        if (materials != null)
        {
            float colorT = EvaluatePulse(blinkSpeed);
            Color currentColor = Color.Lerp(DefaultColor, GetBlinkDisplayColor(), colorT);

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], currentColor);
            }
        }

        if (scaleTarget != null)
        {
            float t = EvaluatePulse(scaleSpeed);
            float scale = Mathf.Lerp(1f, scaleMultiplier, t);
            scaleTarget.localScale = defaultScale * scale;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StartBlinkRpc(float duration = 5f)
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine(duration));
    }

    IEnumerator BlinkRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopBlinkRpc();
        blinkCoroutine = null;
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void StopBlinkRpc()
    {
        isBlinking = false;

        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], DefaultColor);
            }
        }

        if (scaleTarget != null)
            scaleTarget.localScale = defaultScale;
    }

    void ApplyColor(Material mat, Color color)
    {
        if (mat == null) return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.color = color;

        if (useEmission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);
        }
    }

    // PingPong を smootherstep で丸めて、山と谷の切り替わりをやわらかくする。
    float EvaluatePulse(float speed)
    {
        float t = Mathf.PingPong(Time.time * speed, 1f);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    // 点滅色は少し白を混ぜて彩度を落とし、アルファも下げて見た目を柔らかくする。
    Color GetBlinkDisplayColor()
    {
        Color.RGBToHSV(blinkColor, out float hue, out float saturation, out float value);
        Color softened = Color.HSVToRGB(hue, saturation * blinkSaturationScale, value);
        softened.a = blinkAlpha;
        return softened;
    }
#if UNITY_EDITOR
    private void Reset()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }
#endif
}
