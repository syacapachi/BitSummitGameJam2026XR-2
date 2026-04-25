using Unity.Netcode;
using UnityEngine;

public class NEnemySpawnFxEmitter : NetworkBehaviour
{
    [SerializeField] private GameObject spawnFxPrefabAll;
    [SerializeField] private AudioClip spawnSfxAll;
    [SerializeField] private float spawnFxLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float spawnVolumeAll = 1f;
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
        NetFxSpawnUtility.Spawn(
            spawnFxPrefabAll,
            spawnSfxAll,
            position,
            Quaternion.identity,
            spawnFxLifeTimeAll
        );

        // 音
        if (spawnSfxAll != null)
        {
            gameEffectEvent.Invoke(
                new GameEffect(
                    spawnSfxAll,
                    spawnFxPrefabAll,
                    position,
                    volume: spawnVolumeAll
                )
            );
        }
    }
}
