using Unity.Netcode;
using UnityEngine;

public class NEnemyDeathFxEmitter : NetworkBehaviour
{
    [SerializeField] private GameObject deathFxPrefabAll;
    [SerializeField] private AudioClip deathSfxAll;
    [SerializeField] private float deathFxLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float deathVolumeAll = 1f;

    private bool reachedProtectArea = false;

    public override void OnNetworkSpawn()
    {
        reachedProtectArea = false;
    }

    public void MarkReachedProtectAreaServer()
    {
        if (!IsServer) return;

        reachedProtectArea = true;
        SyncReachedProtectAreaClientRpc();
    }

    [ClientRpc]
    private void SyncReachedProtectAreaClientRpc()
    {
        reachedProtectArea = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsClient) return;
        if (reachedProtectArea) return;

        NetFxSpawnUtility.Spawn(
            deathFxPrefabAll,
            deathSfxAll,
            transform.position,
            Quaternion.identity,
            deathFxLifeTimeAll,
            deathVolumeAll);
    }
}