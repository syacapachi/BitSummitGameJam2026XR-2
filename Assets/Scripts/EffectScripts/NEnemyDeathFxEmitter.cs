using Unity.Netcode;
using UnityEngine;

public class NEnemyDeathFxEmitter : NetworkBehaviour
{
    [SerializeField] AudioEffectData deathAudio;
    [SerializeField] FxEffectData deathFx;
    [SerializeField] private GameObject deathFxPrefabAll;
    [SerializeField] private AudioClip deathSfxAll;
    [SerializeField, Range(0f, 1f)] private float deathVolumeAll = 1f;
    [SerializeField] NetworkVariable<bool> reachedProtectArea = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    [SerializeField] GameEffectEvent gameEffectEvent;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
            reachedProtectArea.Value = false;
    }

    public void MarkReachedProtectAreaServer()
    {
        if (!IsServer) return;

        reachedProtectArea.Value = true;
    }


    public override void OnNetworkDespawn()
    {
        if (!IsClient) return;
        if (reachedProtectArea.Value) return;

        //gameEffectEvent.Invoke(GameEffect.CreateCombinedEffect(
        //    deathSfxAll,
        //    deathFxPrefabAll,
        //    transform.position,
        //    deathVolumeAll
        //    ));
        gameEffectEvent.Invoke(
            new GameEffect(
                deathAudio.ToRuntimeData(),
                deathFx.ToRuntimeData(),
                transform.position
            ));
    }
}