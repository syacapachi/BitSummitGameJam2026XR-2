using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;

    [Header("BGM")]
    [SerializeField] AudioClip waitingBGM;
    [SerializeField] AudioClip playingBGM;
    [SerializeField] AudioClip clearBGM;
    [SerializeField] AudioClip gameOverBGM;

    private AudioClip currentClip;

    public void PlayBGM(AudioClip clip, bool loop)
    {
        Debug.Log($"PlayBGM called on {gameObject.name}");
        if (clip == null) return;

        // PlayingŽž‚Ì‚Ý“rØ‚ê–hŽ~
        if (loop && currentClip == clip) return;

        currentClip = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        currentClip = null;
    }

    // ó‘Ô‚²‚Æ‚ÌØ‚è‘Ö‚¦
    public void OnGameStart() => PlayBGM(playingBGM, true);   // © ƒ‹[ƒv
    public void OnGameReset() => PlayBGM(waitingBGM, false);
    public void OnGameClear() => PlayBGM(clearBGM, true);
    public void OnGameOver() => PlayBGM(gameOverBGM, false);
}
