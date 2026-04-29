// using System.Collections;
// using UnityEngine;
// using UnityEngine.Audio;

// public class GameEffectAudioManager : MonoBehaviour
// {

//     [SerializeField] private GameObject audioSourcePrefab;
//     [SerializeField] LocalObjectPoolManager localObjectPool;
//     [SerializeField, Range(0f, 1f)] private float masterSfxVolumeAll = 1f;
//     [Header("Subscribe Event")]
//     [SerializeField] GameEffectEvent gameEffectEvent;
    
//     private void OnEnable()
//     {
//         gameEffectEvent.Register(OnEventRecived);
//     }
//     private void OnDisable()
//     {
//         gameEffectEvent.Unregister(OnEventRecived);
//     }
//     private void OnEventRecived(GameEffect e)
//     {
//         //イベントの処理を追加する場合はここへ
//         if (e.Clip != null)
//         {
//             PlayGameEffect(e);
//         }
//         if(e.FxPrefab != null)
//         {
//             PlayFx(e.FxPrefab,e.Position);
//         }
//     }
//     public void PlayGameEffect(GameEffect effect)
//     {
//         GameObject obj = localObjectPool.Get(audioSourcePrefab);
//         AudioSource audioSource = obj.GetComponent<AudioSource>();
//         audioSource.clip = effect.Clip;
//         audioSource.transform.position = effect.Position;
//         audioSource.volume = effect.Volume * masterSfxVolumeAll;
//         audioSource.pitch = effect.Pitch;
//         audioSource.loop = effect.Loop;
//         //浮動小数点は、== が難しいので比較で行う
//         if(effect.Delay < 0.1f)
//         {
//             StartCoroutine(PlayAndRelease(audioSource));
//         }
//         else
//         {
//             StartCoroutine(PlayAfterDelay(audioSource, effect.Delay));
//         }
//     }
//     private IEnumerator PlayAndRelease(AudioSource source)
//     {
//         source.Play();
//         // ループなら自動解放しない
//         if (source.loop)
//             yield break;

//         //停止を待つ。
//         yield return new WaitWhile(() => source != null && source.isPlaying);

//         if (source != null)
//         {
//             ReturnPool(source);
//         }
//     }
//     private IEnumerator PlayAfterDelay(AudioSource source,float delay)
//     {
//         yield return new WaitForSeconds(delay);
//         yield return PlayAndRelease(source);
//     }
//     private void PlayFx(GameObject fxPrefab,Vector3 pos)
//     {
//         GameObject obj = localObjectPool.Get(fxPrefab);
//         obj.transform.SetPositionAndRotation(pos, Quaternion.identity);
//         localObjectPool.Release(obj, 2f);
//     }
//     private void ReturnPool(AudioSource source)
//     {
//         source.Stop();
//         source.clip = null;
//         localObjectPool.Release(source.gameObject);
//     }
// }

//水野が追加した
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEffectAudioManager : MonoBehaviour
{
    [SerializeField] private GameObject audioSourcePrefab;
    [SerializeField] private LocalObjectPoolManager localObjectPool;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolumeAll = 1f;

    [Header("Subscribe Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    private readonly Dictionary<GameObject, Coroutine> fxReleaseCoroutines = new();

    private void OnEnable()
    {
        if (gameEffectEvent != null)
        {
            gameEffectEvent.Register(OnEventRecived);
        }

    }

    private void OnDisable()
    {
        if (gameEffectEvent != null)
        {
            gameEffectEvent.Unregister(OnEventRecived);
        }
    }

    private void OnEventRecived(GameEffect e)
    {
        // イベントの処理を追加する場合はここへ
        //パターマッチングでAudioEffectとFxEffectを分けて処理する
        if (e.AudioEffect is AudioEffect audio)
        {
            PlayAudioEffect(audio, e.Position, e.Delay);
        }

        if (e.FxEffect is FxEffect fx)
        {
            PlayFxEffect(fx, e.Position, e.Delay);
        }
    }

    public void PlayAudioEffect(AudioEffect effect,Vector3 positon,float delay = 0f)
    {
        GameObject obj = localObjectPool.Get(audioSourcePrefab);
        AudioSource audioSource = obj.GetComponent<AudioSource>();

        audioSource.clip = effect.Clip;
        audioSource.transform.position = positon    ;
        audioSource.volume = effect.Volume * masterSfxVolumeAll;
        audioSource.pitch = effect.Pitch;
        audioSource.loop = effect.Loop;
        if (delay < 0.1f)
        {
            StartCoroutine(PlayAndRelease(audioSource));
        }
        else
        {
            StartCoroutine(PlayAfterDelay(audioSource, delay));
        }
    }

    private IEnumerator PlayAndRelease(AudioSource source)
    {
        if (source == null) yield break;

        source.Play();

        if (source.loop)
            yield break;

        yield return new WaitWhile(() => source != null && source.isPlaying);

        if (source != null)
        {
            ReturnPool(source);
        }
    }

    private IEnumerator PlayAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return PlayAndRelease(source);
    }

    private void PlayFxEffect(FxEffect fxEffect, Vector3 pos, float delay = 0f)
    {
        GameObject obj = localObjectPool.Get(fxEffect.FxPrefab);
        obj.transform.SetPositionAndRotation(pos, Quaternion.identity);

        if (fxReleaseCoroutines.TryGetValue(obj, out Coroutine oldCoroutine))
        {
            if (oldCoroutine != null)
            {
                StopCoroutine(oldCoroutine);
            }
            fxReleaseCoroutines.Remove(obj);
        }

        ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);

        if (particleSystems == null || particleSystems.Length == 0)
        {
            localObjectPool.Release(obj);
            return;
        }

        bool hasLoopParticle = false;

        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;

            var main = ps.main;
            if (main.loop)
            {
                hasLoopParticle = true;
            }

            // 再利用時に前回の見た目が残らないようにする
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        // 単発FXは、全部終わったら自動返却
        if (!hasLoopParticle)
        {
            Coroutine releaseCoroutine = StartCoroutine(ReleaseFxWhenFinished(obj, particleSystems));
            fxReleaseCoroutines[obj] = releaseCoroutine;
            return;
        }

        // ループFXは、寿命が指定されていればその秒数後に自然停止→返却
        if (fxEffect.FxLifeTime > 0f)
        {
            Coroutine releaseCoroutine = StartCoroutine(StopLoopFxAfterLifetime(obj, particleSystems, fxEffect.FxLifeTime));
            fxReleaseCoroutines[obj] = releaseCoroutine;
        }
        // fxLifeTime <= 0 のときは手動停止まで残る
    }

    private IEnumerator ReleaseFxWhenFinished(GameObject obj, ParticleSystem[] particleSystems)
    {
        // 全てのパーティクルシステムが死ぬのを待つ
        yield return new WaitUntil(() =>
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    return false;
                }
            }
            return true;
        });

        if (obj != null)
        {
            localObjectPool.Release(obj);
        }

        fxReleaseCoroutines.Remove(obj);
    }

    private IEnumerator StopLoopFxAfterLifetime(GameObject obj, ParticleSystem[] particleSystems, float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);

        if (obj == null)
        {
            fxReleaseCoroutines.Remove(obj);
            yield break;
        }
        // ループしているパーティクルを全て停止させる
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        // 停止したら、全てのパーティクルが消えるのを待ってから返却
        yield return ReleaseFxWhenFinished(obj, particleSystems);
    }

    private void ReturnPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        localObjectPool.Release(source.gameObject);
    }
}

public class GameEffect
{
    public readonly AudioEffect? AudioEffect;

    public readonly FxEffect? FxEffect;

    /// <summary>発生位置</summary>
    public readonly Vector3 Position;
    /// <summary>遅れ</summary>
    public readonly float Delay;

    public GameEffect(
        AudioClip clip,
        GameObject fxPrefab,
        Vector3 positon,
        float volume = 1.0f,
        float pitch = 1.0f,
        float delay = 0.0f,
        bool loop = false,
        float fxLifeTime = -1f
    )
    {
        this.AudioEffect = new AudioEffect(clip, volume, pitch, loop);
        this.FxEffect = new FxEffect(fxPrefab, fxLifeTime);
        this.Position = positon;
        this.Delay = delay;
    }
    public static GameEffect CreateAudioEffect(AudioClip clip, Vector3 position, float volume = 1.0f, float pitch = 1.0f, float delay = 0.0f, bool loop = false)
    {
        return new GameEffect(clip, null, position, volume, pitch, delay, loop);
    }
    public static GameEffect CreateFxEffect(GameObject fxPrefab, Vector3 position, float delay = 0.0f, float fxLifeTime = -1f)
    {
        return new GameEffect(null, fxPrefab, position, 1.0f, 1.0f, delay, false, fxLifeTime);
    }
    public static GameEffect CreateCombinedEffect(AudioClip clip, GameObject fxPrefab, Vector3 position, float volume = 1.0f, float pitch = 1.0f, float delay = 0.0f, bool loop = false, float fxLifeTime = -1f)
    {
        return new GameEffect(clip, fxPrefab, position, volume, pitch, delay, loop, fxLifeTime);
    }
}
public readonly struct AudioEffect
{
    /// <summary>音</summary>
    public readonly AudioClip Clip;
    /// <summary>大きさ</summary>
    public readonly float Volume;
    /// <summary>ピッチ</summary>
    public readonly float Pitch;
    /// <summary>音をループさせるか</summary>
    public readonly bool Loop;
    public AudioEffect(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, bool loop = false)
    {
        this.Clip = clip;
        this.Volume = volume;
        this.Pitch = pitch;
        this.Loop = loop;
    }
}
public readonly struct FxEffect
{
    /// <summary>particle</summary>
    public readonly GameObject FxPrefab;
    // <summary>
    /// FX寿命
    /// 0以下なら、単発は自動返却・ループは手動停止まで残る
    public readonly float FxLifeTime;
    public FxEffect(GameObject fxPrefab, float fxLifeTime = -1f)
    {
        this.FxPrefab = fxPrefab;
        this.FxLifeTime = fxLifeTime;
    }
}
//水野以上