using UnityEngine;

[CreateAssetMenu(menuName = "Game/Effect/AudioEffect")]
public class AudioEffectDefinition : ScriptableObject
{
    public GameEffectId Id;
    public AudioClip Clip;

    [Header("Default")]
    public float Volume = 1f;
    public float Pitch = 1f;
    public bool Loop = false;
}