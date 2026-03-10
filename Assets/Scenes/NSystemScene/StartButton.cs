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
            Debug.Log("–ğE‚ª‚Ü‚¾Œˆ‚Ü‚Á‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        StartGameServerRpc();
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