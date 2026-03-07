using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRPlayerRoot : PlayerRoot
{
    [SerializeField] XROrigin xrOrigin;
    [SerializeField] InputActionManager inputActionManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            xrOrigin.enabled = true;
            inputActionManager.enabled = true;
        }
        else
        {
            xrOrigin.enabled = false;
            inputActionManager.enabled = false;
        }
    }
}
