using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;

    private bool humanSelected = false;
    private bool ghostSelected = false;

    public void SelectHuman()
    {
        if (!IsServer) return;
        humanSelected = true;
    }

    public void SelectGhost()
    {
        if (!IsServer) return;
        ghostSelected = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Bullet")) return;

        if (!(humanSelected && ghostSelected))
        {
            Debug.Log("役職がまだ決まっていません");
            return;
        }

        StartGameServerRpc();
    }

    [ServerRpc]
    void StartGameServerRpc()
    {
        ManagerLocator.Instance.GameManager.StartGame();
        HideUIClientRpc();
    }

    [ClientRpc]
    void HideUIClientRpc()
    {
        startUI.SetActive(false);
    }
}