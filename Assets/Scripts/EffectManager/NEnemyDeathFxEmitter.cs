using Unity.Netcode;
using UnityEngine;

public class NEnemyDeathFxEmitter : NetworkBehaviour
{
    [SerializeField] private GameObject deathFxPrefabAll;
    [SerializeField] private AudioClip deathSfxAll;
    [SerializeField] private float deathFxLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float deathVolumeAll = 1f;
    [SerializeField] NetworkVariable<bool> reachedProtectArea = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

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

        NetFxSpawnUtility.Spawn(
            deathFxPrefabAll,
            deathSfxAll,
            transform.position,
            Quaternion.identity,
            deathFxLifeTimeAll,
            deathVolumeAll);
    }
}