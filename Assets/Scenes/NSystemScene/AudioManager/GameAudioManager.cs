using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource uiSourceAll;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolumeAll = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (uiSourceAll == null)
        {
            uiSourceAll = gameObject.AddComponent<AudioSource>();
            uiSourceAll.playOnAwake = false;
            uiSourceAll.loop = false;
            uiSourceAll.spatialBlend = 0f;
        }
    }

    public void PlayUI(AudioClip clipAll, float volumeAll = 1f)
    {
        if (clipAll == null || uiSourceAll == null) return;
        uiSourceAll.PlayOneShot(clipAll, volumeAll * masterSfxVolumeAll);
    }

    public void PlayWorld(AudioClip clipAll, Vector3 positionAll, float volumeAll = 1f)
    {
        if (clipAll == null) return;
        AudioSource.PlayClipAtPoint(clipAll, positionAll, volumeAll * masterSfxVolumeAll);
    }
}