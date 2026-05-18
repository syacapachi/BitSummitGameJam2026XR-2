using System;
using UnityEngine;
/// <summary>
/// インスペクター編集用のAudioEffectデータ構造体。クラスのコンストラクタはシリアライズできないため、こっちを経由してAudioEffectを作る
/// </summary>
[CreateAssetMenu(fileName = "AudioEffectData", menuName = "Game/Effect/AudioEffectData")]
public class AudioEffectData : ScriptableObject, IEffect
{
    [SerializeField] EffectType effectType;
    public EffectType EffectType => effectType;
    [SerializeField] AudioClip clip;
    [SerializeField,Range(0f, 1f)]
    float volume = 1f;
    [SerializeField,Range(-3f, 3f)]
    float pitch = 1f;
    [SerializeField] bool loop;
    private AudioEffect cached;
    private bool initialized = false;
    public AudioClip Clip => clip;
    public bool Loop => loop;
    public float Volume => volume;
    public float Pitch => pitch;  
    // AudioEffectへの変換メソッド
    public AudioEffect ToRuntimeData()
    {
        if (!initialized)
        {
            cached = new AudioEffect(this);
            initialized = true;
        }
        return cached;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        initialized = false;
    }
#endif
}
