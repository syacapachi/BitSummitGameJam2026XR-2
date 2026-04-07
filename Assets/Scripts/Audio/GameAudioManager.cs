using UnityEngine;

public class GameAudioManager : MonoBehaviour
{

    [SerializeField] private AudioSource uiSourceAll;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolumeAll = 1f;
    [SerializeField] private AudioSource loopSource;

    private void Awake()
    {
        if (uiSourceAll == null)
        {
            uiSourceAll = gameObject.AddComponent<AudioSource>();
            uiSourceAll.playOnAwake = false;
            uiSourceAll.loop = false;
            uiSourceAll.spatialBlend = 0f;
        }

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = false; // ← 今回はfalse（間隔鳴らし）
            loopSource.spatialBlend = 0f;
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

    public void PlayLoopSE(AudioClip clipAll, float volumeAll = 1f)
    {
        if (clipAll == null || loopSource == null) return;

        loopSource.PlayOneShot(clipAll, volumeAll * masterSfxVolumeAll);
    }
}