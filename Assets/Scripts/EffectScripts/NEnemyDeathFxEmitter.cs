using Unity.Netcode;
using UnityEngine;

public class NEnemyDeathFxEmitter : NetworkBehaviour
{
    [SerializeField] AudioEffectData deathAudio;
    [SerializeField] FxEffectData deathFx;
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
        gameEffectEvent.Invoke(
            new GameEffect(
                deathAudio.ToRuntimeData(),
                deathFx.ToRuntimeData(),
                transform.position
            ));
    }
}