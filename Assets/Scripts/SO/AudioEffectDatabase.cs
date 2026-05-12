using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Effect/EffectDatabase")]
public class AudioEffectDatabase : ScriptableObject
{
    [SerializeField] AudioEffectData[] audioEffects;
    [SerializeField] FxEffectData[] fxEffects;
    private bool isInit = false;
    private readonly Dictionary<EffectType, AudioEffectData> audioDict = new();
    private readonly Dictionary<EffectType, FxEffectData> fxDict = new();
    public IReadOnlyDictionary<EffectType, AudioEffectData> AudioDict
    {
        get
        {
            Init();
            return audioDict;
        }
    }
    public IReadOnlyDictionary<EffectType, FxEffectData> FxDict
    {
        get
        {
            Init();
            return fxDict;
        }
    }

    private void Init()
    {
        if (isInit) return;
        isInit = true;
        audioDict.Clear();

        foreach (var e in audioEffects)
        {
            if (!audioDict.ContainsKey(e.EffectType))
                audioDict.Add(e.EffectType, e);
        }
        foreach (var e in fxEffects)
        {
            if (!fxDict.ContainsKey(e.EffectType))
                fxDict.Add(e.EffectType, e);
        }
    }

    public AudioEffectData GetAudio(EffectType type)
    {
        if (audioDict.TryGetValue(type, out var e))
            return e;

        Debug.LogWarning($"AudioEffect not found: {type}");
        return null;
    }
    public bool TryGetAudio(EffectType type, out AudioEffectData data)
    {
        return audioDict.TryGetValue(type, out data);
    }
    public FxEffectData GetFx(EffectType type)
    {
        if (fxDict.TryGetValue(type, out var e))
            return e;
        Debug.LogWarning($"FxEffect not found: {type}");
        return null;
    }
    public bool TryGetFx(EffectType type, out FxEffectData data)
    {
        return fxDict.TryGetValue(type, out data);
    }
#if UNITY_EDITOR
    // Editor上で変更があったときに再初期化するためのメソッド
    public void OnValidate()
    {
        isInit = false;
    }
#endif
}