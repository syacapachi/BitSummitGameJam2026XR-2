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
    [SerializeField] float blinkSpeed = 8f;
    [SerializeField] bool useEmission = true;
    [SerializeField] float emissionIntensity = 5f;

    [Header("Scale Pulse")]
    [SerializeField] Transform scaleTarget;
    [SerializeField] float scaleSpeed = 3f;
    [SerializeField] float scaleMultiplier = 1.12f;
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
            float colorT = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
            Color currentColor = Color.Lerp(DefaultColor, blinkColor, colorT);

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], currentColor);
            }
        }

        if (scaleTarget != null)
        {
            float t = (Mathf.Sin(Time.time * scaleSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, scaleMultiplier, t);
            scaleTarget.localScale = defaultScale * scale;
        }
    }

    public void StartBlink(float duration = 5f)
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine(duration));
    }

    IEnumerator BlinkRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopBlink();
        blinkCoroutine = null;
    }

    public void StopBlink()
    {
        isBlinking = false;

        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], ownerNormalColor);
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
#if UNITY_EDITOR
    private void Reset()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }
#endif
}