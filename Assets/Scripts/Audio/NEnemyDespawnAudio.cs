using Unity.Netcode;
using UnityEngine;

public class NEnemyDespawnAudio : NetworkBehaviour
{
    [Header("Reference")]
    [SerializeField] private NEnemy nEnemy;

    [Header("Hit Voice / SFX")]
    [SerializeField] AudioEffectData ghostHitAudioData;
    [SerializeField] AudioEffectData demonHitAudioData;

    [Tooltip("Ghost/Demonに該当しない場合、または専用Clipが未設定の場合に使う保険の被弾音です。")]
    [SerializeField] AudioEffectData defaultHitAudioData;

    [SerializeField, Range(0f, 1f)] private float hitVolumeAll = 1f;

    [Tooltip("連続被弾時にボイスが重なりすぎないようにする間隔です。0なら毎回鳴ります。")]
    [SerializeField] private float hitVoiceCooldownAll = 0.25f;

    [Header("Death Voice / SFX")]
    [SerializeField] AudioEffectData ghostDeathAudioData;
    [SerializeField] AudioEffectData demonDeathAudioData;

    [Tooltip("Ghost/Demonに該当しない場合、または専用Clipが未設定の場合に使う保険の死亡音です。")]
    [SerializeField] AudioEffectData defaultDeathAudioData;

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

        gameEffectEvent.Invoke(
            new GameEffect(
                defaultDeathAudioData.ToRuntimeData(),
                transform.position
            )
        );
    }

    private AudioEffectData GetHitClipByEnemyJob()
    {
        if (nEnemy == null)
        {
            return defaultHitAudioData;
        }

        PlayerJob enemyJob = nEnemy.EnemyJob;

        if ((enemyJob & PlayerJob.Ghost) != PlayerJob.Nothing && ghostHitAudioData != null)
        {
            return ghostHitAudioData;
        }

        if ((enemyJob & PlayerJob.Demon) != PlayerJob.Nothing && demonHitAudioData != null)
        {
            return demonHitAudioData;
        }

        return defaultHitAudioData;
    }

    private AudioEffectData GetDeathClipByEnemyJob()
    {
        if (nEnemy == null)
        {
            return defaultDeathAudioData;
        }

        PlayerJob enemyJob = nEnemy.EnemyJob;

        if ((enemyJob & PlayerJob.Ghost) != PlayerJob.Nothing && ghostDeathAudioData != null)
        {
            return ghostDeathAudioData;
        }

        if ((enemyJob & PlayerJob.Demon) != PlayerJob.Nothing && demonDeathAudioData != null)
        {
            return demonDeathAudioData;
        }

        return defaultDeathAudioData;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        nEnemy ??= GetComponent<NEnemy>() ?? GetComponentInParent<NEnemy>();
    }
#endif
}