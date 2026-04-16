using Unity.Netcode;
using UnityEngine;

public class RpcGameEffectAudioManager : NetworkBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject audioSourcePrefab;
    [SerializeField] private LocalObjectPoolManager pool;
    [SerializeField] private AudioEffectDatabase database;
    [SerializeField] GameEffectEvent localEvent;
    [SerializeField] GameEffectDataEvent networkEvent;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;

    private void Awake()
    {
        database.Init();
    }

    private void OnEnable()
    {
        networkEvent.Register(OnEventReceived);
    }

    private void OnDisable()
    {
        networkEvent.Unregister(OnEventReceived);
    }

    // =========================
    // Event入口（サーバーのみ発火）
    // =========================
    private void OnEventReceived(GameEffectData data)
    {
        if (!IsServer) return;

        PlayEffectServer(data);
    }

    // =========================
    // Server → Client
    // =========================
    public void PlayEffectServer(GameEffectData data)
    {
        if (!IsServer) return;

        PlayEffectClientRpc(data);
    }

    [ClientRpc]
    private void PlayEffectClientRpc(GameEffectData data)
    {
        PlayLocal(data);
    }

    // =========================
    // クライアント再生
    // =========================
    private void PlayLocal(GameEffectData data)
    {
        var def = database.Get(data.Id);
        if (def == null || def.Clip == null) return;

        GameObject obj = pool.Get(audioSourcePrefab);
        AudioSource audio = obj.GetComponent<AudioSource>();

        audio.clip = def.Clip;
        audio.transform.position = data.Position;

        audio.volume = (data.Volume > 0 ? data.Volume : def.Volume) * masterSfxVolume;
        audio.pitch  = (data.Pitch  > 0 ? data.Pitch  : def.Pitch);
        audio.loop   = data.Loop || def.Loop;

        if (data.Delay <= 0f)
            audio.Play();
        else
            audio.PlayDelayed(data.Delay);
    }
}

public enum GameEffectId
{
    Shoot,
    Hit,
    Explosion,
    Death
}