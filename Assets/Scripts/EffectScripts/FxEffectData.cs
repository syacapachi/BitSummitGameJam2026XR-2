using UnityEngine;

[CreateAssetMenu(fileName = "FxEffectData", menuName = "Game/Effect/FxEffectData")]
public class FxEffectData : ScriptableObject, IEffect
{
    [SerializeField] EffectType effectType;
    public EffectType EffectType => effectType;
    [SerializeField] GameObject fxPrefab;
    [SerializeField] float loopFxLifeTime;
    public GameObject FxPrefab => fxPrefab;
    public float LoopFxLifeTime => loopFxLifeTime;

    // FxEffectへの変換メソッド
    public FxEffect ToRuntimeData()
    {
        return new FxEffect(this);
    }

}
