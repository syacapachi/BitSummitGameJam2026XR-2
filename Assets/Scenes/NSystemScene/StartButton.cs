using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Bullet"))
        {
            StartGameServerRpc();
        }
    }

    [ServerRpc]
    void StartGameServerRpc()
    {
        NGameManager.Instance.StartGame();
        HideUIClientRpc();
    }

    [ClientRpc]
    void HideUIClientRpc()
    {
        startUI.SetActive(false);
    }
}