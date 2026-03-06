using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;

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

            //InputTracking.GetLocalRotation(XRNode.Head);
            HeadTransfrom.localRotation = playerCamera.transform.localRotation;
        }
    }
}
