using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

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
        Debug.Log("PlayGameEffect called");
        GameObject obj = localObjectPool.Get(audioSourcePrefab);
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = effect.Clip;
        audioSource.transform.position = effect.Position;
        audioSource.volume = effect.Volume * masterSfxVolumeAll;
        audioSource.pitch = effect.Pitch;
        audioSource.loop = effect.Loop;
        //浮動小数点は、== が難しいので比較で行う
        if(effect.Delay < 0.1f)
        {
            StartCoroutine(PlayAndRelease(audioSource));
        }
        else
        {
            StartCoroutine(PlayAfterDelay(audioSource, effect.Delay));
        }
    }
    private IEnumerator PlayAndRelease(AudioSource source)
    {
        source.Play();
        // ループなら自動解放しない
        if (source.loop)
            yield break;

        //停止を待つ。
        yield return new WaitWhile(() => source != null && source.isPlaying);

        if (source != null)
        {
            ReturnPool(source);
        }
    }
    private IEnumerator PlayAfterDelay(AudioSource source,float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return PlayAndRelease(source);
    }
    private void ReturnPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        localObjectPool.Release(source.gameObject);
    }
}
public readonly struct GameEffect
{
    /// <summary>
    /// 音
    /// </summary>
    public readonly AudioClip Clip;
    /// <summary>
    /// particle
    /// </summary>
    public readonly GameObject FxPrefab;
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
    /// <summary>
    /// ループさせるか
    /// </summary>
    public readonly bool Loop;

    public GameEffect(
        AudioClip clip,
        GameObject fxPrefab,
        Vector3 positon,
        float volume = 1.0f,
        float pitch = 1.0f,
        float delay = 0.0f,
        bool loop = false
        )
    {
        this.Clip = clip;
        this.FxPrefab = fxPrefab;
        this.Position = positon;
        this.Volume = volume;
        this.Pitch = pitch;
        this.Delay = delay;
        this.Loop = loop;
    }
}