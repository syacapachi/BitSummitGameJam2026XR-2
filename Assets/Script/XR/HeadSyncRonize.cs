using Unity.Netcode;
using UnityEngine;

public class HeadSyncRonize : NetworkBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] Transform HeadTransfrom;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            this.enabled = false;
        }
    }
    private void Update()
    {
        if (IsOwner)
        {
            HeadTransfrom.rotation = playerCamera.gameObject.transform.rotation;
        }
    }
}
