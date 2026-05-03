using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class NEnemyDespawnAudio : NetworkBehaviour
{
    [Header("Reference")]
    [SerializeField] private NEnemy nEnemy;

    [Header("Hit Voice / SFX")]
    [SerializeField] AudioEffectData ghostHitAudioData;
    [SerializeField] AudioEffectData demonHitAudioData;
    [SerializeField] private AudioClip ghostHitClipAll;
    [SerializeField] private AudioClip demonHitClipAll;

    [Tooltip("Ghost/Demonに該当しない場合、または専用Clipが未設定の場合に使う保険の被弾音です。")]
    [SerializeField] AudioEffectData defaultHitAudioData;
    [SerializeField] private AudioClip defaultHitClipAll;

    [SerializeField, Range(0f, 1f)] private float hitVolumeAll = 1f;

    [Tooltip("連続被弾時にボイスが重なりすぎないようにする間隔です。0なら毎回鳴ります。")]
    [SerializeField] private float hitVoiceCooldownAll = 0.25f;

    [Header("Death Voice / SFX")]
    [SerializeField] AudioEffectData ghostDeathAudioData;
    [SerializeField] AudioEffectData demonDeathAudioData;
    [SerializeField] private AudioClip ghostDeathClipAll;
    [SerializeField] private AudioClip demonDeathClipAll;

    [Tooltip("Ghost/Demonに該当しない場合、または専用Clipが未設定の場合に使う保険の死亡音です。")]
    [SerializeField] AudioEffectData defaultDeathAudioData;
    [SerializeField] private AudioClip defaultDeathClipAll;

    [SerializeField, Range(0f, 1f)] private float deathVolumeAll = 1f;

    [Header("Publish Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    private bool reachedGoal = false;
    private bool playedDeathAudio = false;
    private float lastHitVoiceTimeServer = -999f;

    private void Awake()
    {
        if (nEnemy == null)
        {
            nEnemy = GetComponent<NEnemy>() ?? GetComponentInParent<NEnemy>();
        }
    }

    public void MarkReachedGoalServer()
    {
        reachedGoal = true;
    }

    public override void OnNetworkSpawn()
    {
        reachedGoal = false;
        playedDeathAudio = false;
        lastHitVoiceTimeServer = -999f; 
    }

    /// <summary>
    /// 敵が被弾したときに、Server側から呼ぶ。
    /// 全クライアントで敵の被弾ボイスを再生する。
    /// </summary>
    public void PlayHitVoiceServer()
    {
        if (!IsServer) return;
        if (gameObject == null) return;

        if (hitVoiceCooldownAll > 0f)
        {
            if (Time.time - lastHitVoiceTimeServer < hitVoiceCooldownAll)
            {
                return;
            }
        }

        lastHitVoiceTimeServer = Time.time;

        PlayHitVoiceRpc(transform.position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayHitVoiceRpc(Vector3 position)
    {
        if (gameEffectEvent == null) return;

        AudioClip hitClip = GetHitClipByEnemyJob();
        if (hitClip == null) return;

        gameEffectEvent.Invoke(
            new GameEffect(
                ghostHitAudioData.ToRuntimeData(),
                position
            )
        );
    }

    public override void OnNetworkDespawn()
    {
        if (reachedGoal) return;
        if (playedDeathAudio) return;
        playedDeathAudio = true;

        if (gameEffectEvent == null) return;

        AudioClip deathClip = GetDeathClipByEnemyJob();
        if (deathClip == null) return;

        gameEffectEvent.Invoke(
            new GameEffect(
                defaultDeathAudioData.ToRuntimeData(),
                transform.position
            )
        );
    }

    private AudioClip GetHitClipByEnemyJob()
    {
        if (nEnemy == null)
        {
            return defaultHitClipAll;
        }

        PlayerJob enemyJob = nEnemy.EnemyJob;

        if ((enemyJob & PlayerJob.Ghost) != PlayerJob.Nothing && ghostHitClipAll != null)
        {
            return ghostHitClipAll;
        }

        if ((enemyJob & PlayerJob.Demon) != PlayerJob.Nothing && demonHitClipAll != null)
        {
            return demonHitClipAll;
        }

        return defaultHitClipAll;
    }

    private AudioClip GetDeathClipByEnemyJob()
    {
        if (nEnemy == null)
        {
            return defaultDeathClipAll;
        }

        PlayerJob enemyJob = nEnemy.EnemyJob;

        if ((enemyJob & PlayerJob.Ghost) != PlayerJob.Nothing && ghostDeathClipAll != null)
        {
            return ghostDeathClipAll;
        }

        if ((enemyJob & PlayerJob.Demon) != PlayerJob.Nothing && demonDeathClipAll != null)
        {
            return demonDeathClipAll;
        }

        return defaultDeathClipAll;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        nEnemy ??= GetComponent<NEnemy>() ?? GetComponentInParent<NEnemy>();
    }
#endif
}