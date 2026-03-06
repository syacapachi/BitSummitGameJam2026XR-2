using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource uiSource;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f; // 2D音
        }
    }

    public void PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip, volume * masterSfxVolume);
    }

    public void PlayWorld(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * masterSfxVolume);
    }
}