using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            StartGameServerRpc();
        }
    }

    [ServerRpc]
    void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        NGameManager.Instance.StartGame();
    }
}