using UnityEngine;
using Unity.Netcode;

public class StartButton : NetworkBehaviour
{
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject humanUI;
    [SerializeField] GameObject ghostUI;


    public void SelectHuman()
    {
        if(IsServer) return;
        ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.propaty.Job = PlayerPropaty.PlayerJob.Human;
        humanUI.SetActive(false);
        Debug.Log("Human");
    }

    public void SelectGhost()
    {
        if (IsServer) return;
        ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.propaty.Job = PlayerPropaty.PlayerJob.Ghost;
        ghostUI.SetActive(false);
        Debug.Log("Ghost");
    }

    public void SelectStartGame()
    {
        StartGameRpc();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Bullet")) return;

        StartGameRpc();
    }

    [Rpc(SendTo.Server)]
    void StartGameRpc()
    {
        Debug.Log("[Start Game Rpc]");
        ManagerLocator.Instance.AllGameManager.StartGame();
        HideUIClientRpc();
    }

    [ClientRpc]
    void HideUIClientRpc()
    {
        startUI.SetActive(false);
        humanUI.SetActive(false);
        ghostUI.SetActive(false);
    }
}