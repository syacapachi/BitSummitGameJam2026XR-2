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
    private AudioClip currentClip;

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
    }
    private void OnDisable()
    {
        OnGameStateChangeRpcEvent.Unregister(OnGameStateChanged);
    }
    public void StopBGM()
    {
        bgmSource.Stop();
        currentClip = null;
    }
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                PlayBGM(playingBGM, true);
                break;
            case GameState.Initializing:
                PlayBGM(waitingBGM, false);
                break;
            case GameState.GameClear:
                PlayBGM(clearBGM, true); break;
            case GameState.GameOver:
                PlayBGM(gameOverBGM, false);break;
        }
    }
}
