using System;
using System.Collections;
using UnityEngine;

public class Crystal : MonoBehaviour,IDamageReciever
{
    [Header("Target")]
    public GameObject crystal; // ← 操作対象

    [Header("Effect")]
    public GameObject breakEffectPrefab;
    public GameObject hitEffectPrefab;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip breakSE;
    public AudioClip hitSE;
    [Header("Subscribe event")]
    [SerializeField] VoidEvent OnbulletComeRpcEvent;
    [SerializeField] GameStateEvent GameStateChangeRpcEvent;
    public GameObject GameObject => crystal;

    public float CurrentHealth => throw new System.NotImplementedException();

    public float MaxHealth => throw new System.NotImplementedException();

    void OnEnable()
    {
        OnbulletComeRpcEvent.Register(OnBulletCome);
        GameStateChangeRpcEvent.Register(OnGameStateChanged);
    }

    void OnDisable()
    {
        OnbulletComeRpcEvent.Unregister(OnBulletCome);
        GameStateChangeRpcEvent.Unregister(OnGameStateChanged);
    }
    private void OnGameStateChanged(GameState state)
    {
        switch(state)
        {
            case GameState.GameOver:
                OnGameOverChanged(); break;
            case GameState.Initializing:
            case GameState.Home:
                OnGameReset(); break;
            default: break;
        }
    }
    void OnGameOverChanged()
    {
        Broken();
    }

    void Broken()
    {
        Debug.Log("Crystal Broken");

        PlaySound();
        PlayEffect();
        PlayAnimation();
    }
    void OnBulletCome()
    {
        PlayHitEffect();
        PlayHitSound();
    }

    void PlaySound()
    {
        if (audioSource != null && breakSE != null)
        {
            audioSource.PlayOneShot(breakSE);
        }
    }

    void PlayEffect()
    {
        if (breakEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                breakEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, 2f); // ← 2秒後に消す
        }
    }

    void PlayAnimation()
    {
        StartCoroutine(AnimationCoroutine());
    }

    IEnumerator AnimationCoroutine()
    {
        // 今は簡易（後でここにアニメーション入れる）
        yield return new WaitForSeconds(0.2f);

        if (crystal != null)
        {
            crystal.SetActive(false);
        }
    }

    void PlayHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, 2f); // ← 2秒後に消す
        }
    }

    void PlayHitSound()
    {
        if (audioSource != null && hitSE != null)
        {
            audioSource.PlayOneShot(hitSE,0.3f);
        }
    }

    public void TakeDamage(IDamageSender sender, float damage)
    {
        throw new System.NotImplementedException();
    }

    void OnGameReset()
    {
        Restore();
    }

    void Restore()
    {
        Debug.Log("Crystal Restore");

        StopAllCoroutines();

        if (crystal != null)
        {
            crystal.SetActive(true);
        }
    }
}