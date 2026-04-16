using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Effect/AudioEffectDatabase")]
public class AudioEffectDatabase : ScriptableObject
{
    public List<AudioEffectDefinition> effects;

    private Dictionary<GameEffectId, AudioEffectDefinition> dict;

    public void Init()
    {
        dict = new Dictionary<GameEffectId, AudioEffectDefinition>();

        foreach (var e in effects)
        {
            if (!dict.ContainsKey(e.Id))
                dict.Add(e.Id, e);
        }
    }

    public AudioEffectDefinition Get(GameEffectId id)
    {
        if (dict.TryGetValue(id, out var e))
            return e;

        Debug.LogWarning($"AudioEffect not found: {id}");
        return null;
    }
}