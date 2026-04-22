using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyFxRule))]
public class NEnemyImpactFxReceiver : NetworkBehaviour
{
    [Header("Valid Hit FX / SFX")]
    [SerializeField] private GameObject validHitFxPrefabAll;
    [SerializeField] private AudioClip validHitSfxAll;
    [SerializeField] private float validHitLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float validHitVolumeAll = 1f;

    [Header("Invalid Hit FX / SFX")]
    [SerializeField] private GameObject invalidHitFxPrefabAll;
    [SerializeField] private AudioClip invalidHitSfxAll;
    [SerializeField] private float invalidHitLifeTimeAll = 2f;
    [SerializeField, Range(0f, 1f)] private float invalidHitVolumeAll = 1f;

    private EnemyFxRule fxRuleServer;

    private void Awake()
    {
        fxRuleServer = GetComponent<EnemyFxRule>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        TryHandleBulletHitServer(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        TryHandleBulletHitServer(collision.collider);
    }

    private void TryHandleBulletHitServer(Collider other)
    {
        NBullet bullet = other.GetComponent<NBullet>() ?? other.GetComponentInParent<NBullet>();
        if (bullet == null) return;

        NBulletFxState bulletFxState = bullet.GetComponent<NBulletFxState>();
        if (bulletFxState != null && bulletFxState.ImpactFxPlayed) return;
        if (bulletFxState != null) bulletFxState.ImpactFxPlayed = true;

        Vector3 fxPosition = other.ClosestPoint(transform.position);
        if (fxPosition == Vector3.zero)
        {
            fxPosition = other.transform.position;
        }

        PlayerJob shooterJob = ResolveShooterJobServer(bullet.ShooterId);
        bool isEffective = fxRuleServer == null || fxRuleServer.IsEffectiveFor(shooterJob);

        if (isEffective)
        {
            PlayValidHitClientRpc(fxPosition);
        }
        else
        {
            ulong[] targetIds = CollectVisibleClientIdsServer();
            if (targetIds.Length == 0) return;

            PlayInvalidHitClientRpc(fxPosition);
        }
    }

    private PlayerJob ResolveShooterJobServer(ulong shooterId)
    {
        if (NetworkManager.Singleton == null) return PlayerJob.Nothing;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var client))
            return PlayerJob.Nothing;

        if (client.PlayerObject == null) return PlayerJob.Nothing;

        SyncroPropaty propaty = client.PlayerObject.GetComponentInChildren<SyncroPropaty>();
        if (propaty == null) return PlayerJob.Nothing;

        return propaty.Job;
    }

    private ulong[] CollectVisibleClientIdsServer()
    {
        List<ulong> ids = new List<ulong>();

        if (NetworkManager.Singleton == null) return ids.ToArray();

        foreach (var pair in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkObject playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;

            SyncroPropaty propaty = playerObject.GetComponentInChildren<SyncroPropaty>();
            if (propaty == null) continue;

            if (fxRuleServer.IsVisibleTo(propaty.Job))
            {
                ids.Add(pair.Key);
            }
        }

        return ids.ToArray();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayValidHitClientRpc(Vector3 position)
    {
        NetFxSpawnUtility.Spawn(
            validHitFxPrefabAll,
            validHitSfxAll,
            position,
            Quaternion.identity,
            validHitLifeTimeAll,
            validHitVolumeAll);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayInvalidHitClientRpc(Vector3 position)
    {
        NetFxSpawnUtility.Spawn(
            invalidHitFxPrefabAll,
            invalidHitSfxAll,
            position,
            Quaternion.identity,
            invalidHitLifeTimeAll,
            invalidHitVolumeAll);
    }
}