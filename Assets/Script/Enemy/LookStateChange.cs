using UnityEngine;
using Unity.Netcode;

public class LookStateChange : NetworkBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;

    public override void OnNetworkSpawn()
    {
        ManagerLocator.Instance.PlayerManager.OnOwnerJobChanged += OnJobChangedHandle;
    }
    public override void OnNetworkDespawn()
    {
        ManagerLocator.Instance.PlayerManager.OnOwnerJobChanged -= OnJobChangedHandle;
    }
    private void OnJobChangedHandle(PlayerPropaty.PlayerJob job)
    {
        meshRenderer.enabled = job == PlayerPropaty.PlayerJob.Ghost;
    }
}
