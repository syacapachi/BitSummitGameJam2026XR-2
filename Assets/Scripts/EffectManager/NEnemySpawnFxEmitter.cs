using Unity.Netcode;
using UnityEngine;

public class NEnemySpawnFxEmitter : NetworkBehaviour
{
    [SerializeField] private GameObject spawnFxPrefabAll;
    [SerializeField] private AudioClip spawnSfxAll;
    [SerializeField] private float spawnFxLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float spawnVolumeAll = 1f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // サーバーが「出現したぞ」と通知
        PlaySpawnFxClientRpc(transform.position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlaySpawnFxClientRpc(Vector3 position)
    {
        NetFxSpawnUtility.Spawn(
            spawnFxPrefabAll,
            spawnSfxAll,
            position,
            Quaternion.identity,
            spawnFxLifeTimeAll,
            spawnVolumeAll);
    }
}
