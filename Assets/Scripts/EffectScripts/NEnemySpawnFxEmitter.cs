using Unity.Netcode;
using UnityEngine;

public class NEnemySpawnFxEmitter : NetworkBehaviour
{
    [SerializeField] private GameObject spawnFxPrefabAll;
    [SerializeField] AudioEffectData audioEffectData;
    [SerializeField] FxEffectData fxEffect;
    [Header("Publish Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // サーバーが「出現したぞ」と通知
        PlaySpawnFxClientRpc(transform.position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlaySpawnFxClientRpc(Vector3 position)
    {
        // FX
        //NetFxSpawnUtility.Spawn(
        //    spawnFxPrefabAll,
        //    spawnSfxAll,
        //    position,
        //    Quaternion.identity,
        //    spawnFxLifeTimeAll
        //);

        // 音
        gameEffectEvent.Invoke(
            new GameEffect(audioEffectData.ToRuntimeData(), fxEffect.ToRuntimeData(), position)
        );
    }
}
