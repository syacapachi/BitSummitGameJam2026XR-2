using UnityEngine;

public class MarkerBlinkEffect : MonoBehaviour
{
    [Header("Color Blink")]
    [SerializeField] Renderer[] targetRenderers;
    [SerializeField] Color normalColor = Color.green;
    [SerializeField] Color blinkColor = Color.white;
    [SerializeField] float blinkSpeed = 8f;
    [SerializeField] bool useEmission = true;

    [Header("Scale Pulse")]
    [SerializeField] Transform scaleTarget;   // ここを拡大縮小する
    [SerializeField] float scaleSpeed = 6f;
    [SerializeField] float scaleMultiplier = 1.2f; // 最大何倍まで大きくするか

    Material[] materials;
    bool isBlinking = false;

    Vector3 defaultScale;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }

        defaultScale = scaleTarget.localScale;

        // 各インスタンス専用Materialを持つ
        materials = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            materials[i] = targetRenderers[i].material;
            ApplyColor(materials[i], normalColor);
        }
    }

    private void Update()
    {
        if (!isBlinking) return;

        // ===== 色の点滅 =====
        if (materials != null)
        {
            float colorT = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            Color currentColor = Color.Lerp(normalColor, blinkColor, colorT);

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], currentColor);
            }
        }

        // ===== Scaleの脈動 =====
        if (scaleTarget != null)
        {
            float scaleT = Mathf.PingPong(Time.time * scaleSpeed, 1f);
            float scale = Mathf.Lerp(1f, scaleMultiplier, scaleT);
            scaleTarget.localScale = defaultScale * scale;
        }
    }

    public void StartBlink()
    {
        isBlinking = true;
    }

    public void StopBlink()
    {
        isBlinking = false;

        // 色を元に戻す
        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                ApplyColor(materials[i], normalColor);
            }
        }

        // Scaleを元に戻す
        if (scaleTarget != null)
        {
            scaleTarget.localScale = defaultScale;
        }
    }

    void ApplyColor(Material mat, Color color)
    {
        if (mat.HasProperty("_Color"))
        {
            mat.color = color;
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (useEmission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
        }
    }
}