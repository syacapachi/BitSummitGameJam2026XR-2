using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;

    [Header("BGM")]
    [SerializeField] AudioClip waitingBGM;
    [SerializeField] AudioClip playingBGM;
    [SerializeField] AudioClip clearBGM;
    [SerializeField] AudioClip gameOverBGM;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent OnGameStateChangeRpcEvent;
    [SerializeField] BoolEvent WarningStateEvent;
    private AudioClip currentClip;
    bool playFastNext = false;
    bool isWarning = false;

    public void PlayBGM(AudioClip clip, bool loop)
    {
        Debug.Log($"PlayBGM called on {gameObject.name}");
        if (clip == null) return;

        // Playing時のみ途切れ防止
        if (loop && currentClip == clip) return;

        currentClip = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }
    private void OnEnable()
    {
        OnGameStateChangeRpcEvent.Register(OnGameStateChanged);
        WarningStateEvent.Register(OnWarningStateChanged);
    }
    private void OnDisable()
    {
        OnGameStateChangeRpcEvent.Unregister(OnGameStateChanged);
        WarningStateEvent.Unregister(OnWarningStateChanged);
    }
    public void StopBGM()
    {
        bgmSource.Stop();
        currentClip = null;
    }
    private void OnGameStateChanged(GameState state)
    {
        if (isWarning) return;
        switch (state)
        {
            case GameState.Playing:
                bgmSource.pitch = 1f;
                PlayBGM(playingBGM, true);
                break;
            case GameState.Initializing:
                bgmSource.pitch = 1f;
                PlayBGM(waitingBGM, false);
                break;
            case GameState.GameClear:
                bgmSource.pitch = 1f;
                PlayBGM(clearBGM, true); break;
            case GameState.GameOver:
                bgmSource.pitch = 1f;
                PlayBGM(gameOverBGM, false);break;
        }
    }

    void OnWarningStateChanged(bool active)
    {
        isWarning = active;

        if (active)
        {
            StopBGM();
        }
        else
        {
            bgmSource.pitch = 1.3f;
            PlayBGM(playingBGM, true);
        }
    }
}
