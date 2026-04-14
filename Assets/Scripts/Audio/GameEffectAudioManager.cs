using UnityEngine;

public class GameEffectAudioManager : MonoBehaviour
{

    [SerializeField] private GameObject audioSourcePrefab;
    [SerializeField] LocalObjectPoolManager localObjectPool;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolumeAll = 1f;
    [Header("Subscribe Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    
    private void OnEnable()
    {
        gameEffectEvent.Register(OnEventRecived);
    }
    private void OnDisable()
    {
        gameEffectEvent.Unregister(OnEventRecived);
    }
    private void OnEventRecived(GameEffect e)
    {
        //イベントの処理を追加する場合はここへ
        PlayGameEffect(e);
    }
    public void PlayGameEffect(GameEffect effect)
    {
        GameObject obj = localObjectPool.Get(audioSourcePrefab);
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = effect.Clip;
        audioSource.transform.position = effect.Position;
        audioSource.volume = effect.Volume * masterSfxVolumeAll;
        audioSource.pitch = effect.Pitch;
        if(effect.Delay == 0f)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.PlayDelayed(effect.Delay);
        }
    }
}
public readonly struct GameEffect
{
    /// <summary>
    /// 音
    /// </summary>
    public readonly AudioClip Clip;
    /// <summary>
    /// 発生位置
    /// </summary>
    public readonly Vector3 Position;
    /// <summary>
    /// 大きさ
    /// </summary>
    public readonly float Volume;
    /// <summary>
    /// ピッチ
    /// </summary>
    public readonly float Pitch;
    /// <summary>
    /// 遅れ
    /// </summary>
    public readonly float Delay;

    public GameEffect(
        AudioClip clip,
        Vector3 positon,
        float volume = 1.0f,
        float pitch = 1.0f,
        float delay = 0.0f)
    {
        this.Clip = clip;
        this.Position = positon;
        this.Volume = volume;
        this.Pitch = pitch;
        this.Delay = delay;
    }
}