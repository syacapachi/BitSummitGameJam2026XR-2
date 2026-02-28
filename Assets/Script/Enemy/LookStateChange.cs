using UnityEngine;
using Unity.Netcode;

public class LookStateChange : NetworkBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] PlayerPropaty.PlayerJob lookableJob;
    public override void OnNetworkSpawn()
    {
        PlayerManager playerManager = ManagerLocator.Instance.PlayerManager;
        playerManager.OnOwnerJobChanged += OnJobChangedHandle;
        if (playerManager.OwnerPlayer != null) 
        {
            OnJobChangedHandle(playerManager.OwnerPlayer.propaty.Job);
        }
    }
    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.PlayerManager.OnOwnerJobChanged -= OnJobChangedHandle;
    }
    private void OnJobChangedHandle(PlayerPropaty.PlayerJob job)
    {   
        meshRenderer.enabled = (job & lookableJob) != 0;
    }
}
