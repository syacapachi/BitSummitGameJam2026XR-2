using UnityEngine;
using Unity.Netcode;

public class RoleButton : NetworkBehaviour
{
    public enum RoleType
    {
        Human,
        Ghost
    }

    [SerializeField] RoleType role;
    [SerializeField] StartButton startButton;
    [SerializeField] GameObject RoleUI;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if(!other.TryGetComponent<IDamageSender>(out var damageSender))
        {
            return;
        }

        if (damageSender is not NBullet bullet) return;

        ulong shooterId = bullet.ResultCollector.ClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var client))
            return;

        var player = client.PlayerObject.GetComponentInChildren<SyncroPropaty>();
        if (player == null) return;

        SetRole(player);
        HideClientRpc();
    }

    void SetRole(SyncroPropaty player)
    {
        Debug.Log("SetRole called");

        if (role == RoleType.Human)
        {
            Debug.Log("Setting Human");
            player.Job = PlayerJob.Demon;
            startButton.SelectHuman();
        }
        else
        {
            Debug.Log("Setting Ghost");
            player.Job = PlayerJob.Ghost;
            startButton.SelectGhost();
        }

        Debug.Log("Current Job: " + player.Job);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void HideClientRpc()
    {
        Debug.Log("HideClientRpc called");
        RoleUI.SetActive(false);
    }
}