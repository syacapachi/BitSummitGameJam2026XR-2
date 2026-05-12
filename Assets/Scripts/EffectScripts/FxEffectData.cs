using Syacapachi.Attribute;
using UnityEngine;

[CreateAssetMenu(fileName = "FxEffectData", menuName = "Game/Effect/FxEffectData")]
public class FxEffectData : ScriptableObject, IEffect
{
    [SerializeField] EffectType effectType;
    public EffectType EffectType => effectType;
    [SerializeField] GameObject fxPrefab;
    [SerializeField] float loopFxLifeTime;
    [SerializeField] bool isLayerOverride = false;
    [SerializeField, EnableIf(nameof(isLayerOverride)), SingleFlagOnly]
    LayerMask layerOverride = 1;
    public GameObject FxPrefab => fxPrefab;
    public float LoopFxLifeTime => loopFxLifeTime;
    private bool isApplydLayer = false;
    void ApplyLayer()
    {
        if(isApplydLayer) return;
        isApplydLayer = true;
        if (isLayerOverride)
        {
            int layer = Mathf.RoundToInt(Mathf.Log(layerOverride.value, 2));
            ApplyLayerRecursively(fxPrefab.transform, layer);
        }
    }
    void ApplyLayerRecursively(Transform transform, int layer)
    {
        transform.gameObject.layer = layer;
        foreach (Transform child in transform)
        {
            ApplyLayerRecursively(child, layer);
        }
    }
    // FxEffectへの変換メソッド
    public FxEffect ToRuntimeData()
    {
        ApplyLayer();
        return new FxEffect(this);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isLayerOverride && layerOverride.value == 0)
        {
            Debug.LogWarning("CollidersLayer override is enabled but no layer is selected. Please select a layer.");
        }
        isApplydLayer = false;
    }
#endif
}
