using UnityEngine;

[CreateAssetMenu(menuName = "Game/Effect")]
public class AudioEffectDefinition : ScriptableObject
{
    public EffectType type;
    public AudioClip Clip;

    [Header("Default")]
    public float Volume = 1f;
    public float Pitch = 1f;
    public bool Loop = false;
}