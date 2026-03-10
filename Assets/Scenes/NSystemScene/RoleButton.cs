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
        if (!other.CompareTag("Bullet")) return;

        var bullet = other.GetComponentInParent<NBullet>();
        if (bullet == null) return;

        ulong shooterId = bullet.shooterId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out var client))
            return;

        var player = client.PlayerObject.GetComponentInChildren<PlayerPropaty>();
        if (player == null) return;

        SetRole(player);
        HideClientRpc();
    }

    void SetRole(PlayerPropaty player)
    {
        Debug.Log("SetRole called");

        if (role == RoleType.Human)
        {
            Debug.Log("Setting Human");
            player.Job = PlayerPropaty.PlayerJob.Human;
            startButton.SelectHuman();
        }
        else
        {
            Debug.Log("Setting Ghost");
            player.Job = PlayerPropaty.PlayerJob.Ghost;
            startButton.SelectGhost();
        }

        Debug.Log("Current Job: " + player.Job);
    }

    [ClientRpc]
    void HideClientRpc()
    {
        Debug.Log("HideClientRpc called");
        RoleUI.SetActive(false);
    }
}