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
// public readonly struct GameEffect
// {
//     /// <summary>
//     /// 音
//     /// </summary>
//     public readonly AudioClip Clip;
//     /// <summary>
//     /// particle
//     /// </summary>
//     public readonly GameObject FxPrefab;
//     /// <summary>
//     /// 発生位置
//     /// </summary>
//     public readonly Vector3 Position;
//     /// <summary>
//     /// 大きさ
//     /// </summary>
//     public readonly float Volume;
//     /// <summary>
//     /// ピッチ
//     /// </summary>
//     public readonly float Pitch;
//     /// <summary>
//     /// 遅れ
//     /// </summary>
//     public readonly float Delay;
//     /// <summary>
//     /// ループさせるか
//     /// </summary>
//     public readonly bool Loop;

//     public GameEffect(
//         AudioClip clip,
//         GameObject fxPrefab,
//         Vector3 positon,
//         float volume = 1.0f,
//         float pitch = 1.0f,
//         float delay = 0.0f,
//         bool loop = false
//         )
//     {
//         this.Clip = clip;
//         this.FxPrefab = fxPrefab;
//         this.Position = positon;
//         this.Volume = volume;
//         this.Pitch = pitch;
//         this.Delay = delay;
//         this.Loop = loop;
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
        if (e.Clip != null)
        {
            PlayGameEffect(e);
        }

        if (e.FxPrefab != null)
        {
            PlayFx(e.FxPrefab, e.Position, e.FxLifeTime);
        }
    }

    public void PlayGameEffect(GameEffect effect)
    {
        GameObject obj = localObjectPool.Get(audioSourcePrefab);
        AudioSource audioSource = obj.GetComponent<AudioSource>();

        audioSource.clip = effect.Clip;
        audioSource.transform.position = effect.Position;
        audioSource.volume = effect.Volume * masterSfxVolumeAll;
        audioSource.pitch = effect.Pitch;
        audioSource.loop = effect.Loop;

        if (effect.Delay < 0.1f)
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

    private void PlayFx(GameObject fxPrefab, Vector3 pos, float fxLifeTime = -1f)
    {
        GameObject obj = localObjectPool.Get(fxPrefab);
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
            localObjectPool.Release(obj, 2f);
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
        if (fxLifeTime > 0f)
        {
            Coroutine releaseCoroutine = StartCoroutine(StopLoopFxAfterLifetime(obj, particleSystems, fxLifeTime));
            fxReleaseCoroutines[obj] = releaseCoroutine;
        }
        // fxLifeTime <= 0 のときは手動停止まで残る
    }

    private IEnumerator ReleaseFxWhenFinished(GameObject obj, ParticleSystem[] particleSystems)
    {
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

        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

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

    private void ReturnPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        localObjectPool.Release(source.gameObject);
    }
}

public readonly struct GameEffect
{
    /// <summary>音</summary>
    public readonly AudioClip Clip;

    /// <summary>particle</summary>
    public readonly GameObject FxPrefab;

    /// <summary>発生位置</summary>
    public readonly Vector3 Position;

    /// <summary>大きさ</summary>
    public readonly float Volume;

    /// <summary>ピッチ</summary>
    public readonly float Pitch;

    /// <summary>遅れ</summary>
    public readonly float Delay;

    /// <summary>音をループさせるか</summary>
    public readonly bool Loop;

    /// <summary>
    /// FX寿命
    /// 0以下なら、単発は自動返却・ループは手動停止まで残る
    /// </summary>
    public readonly float FxLifeTime;

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
        this.Clip = clip;
        this.FxPrefab = fxPrefab;
        this.Position = positon;
        this.Volume = volume;
        this.Pitch = pitch;
        this.Delay = delay;
        this.Loop = loop;
        this.FxLifeTime = fxLifeTime;
    }
}
//水野以上