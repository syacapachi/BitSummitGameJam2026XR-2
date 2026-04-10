using UnityEngine;

public class AudioDebug : MonoBehaviour
{
    void Update()
    {
        var sources = FindObjectsOfType<AudioSource>();

        foreach (var s in sources)
        {
            if (s.isPlaying)
            {
                Debug.Log($"Playing: {s.gameObject.name} | Clip: {s.clip}");
            }
        }
    }
}