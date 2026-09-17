using System;
using Unity.Netcode;
using UnityEngine;

public class LocalPlayerTeleport : MonoBehaviour
{
    [SerializeField] VoidEvent gameResetEvent;
    [SerializeField] Transform hostSpawnPoint;
    [SerializeField] Transform clientSpawnPoint;
    [SerializeField] Transform playerRootTransform;
    [SerializeField] bool isTeleporting;

    private void OnEnable()
    {
        gameResetEvent.Register(OnReset);
    }

    private void OnDisable()
    {
        gameResetEvent.Unregister(OnReset);
    }

    private void OnReset()
    {
        if (!isTeleporting) return;

        Transform spawnPoint =
                NetworkManager.Singleton.IsHost
                ? hostSpawnPoint
            : clientSpawnPoint;

         Vector3 currentPos = playerRootTransform.position;

         playerRootTransform.position = new Vector3(
                spawnPoint.position.x,
                currentPos.y,
                spawnPoint.position.z
         );
    }
    
}