using Unity.XR.CoreUtils;
using UnityEngine;

public class XRPlayerRoot : PlayerRoot
{
    [SerializeField] XROrigin xrOrigin;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            xrOrigin.enabled = true;
        }
        else
        {
            xrOrigin.enabled = false;
        }
    }
}
