using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRPlayerRoot : PlayerRoot
{
    [SerializeField] XROrigin xrOrigin;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            xrOrigin.gameObject.SetActive(true);
        }
        else
        {
            xrOrigin.gameObject.SetActive(false);
        }
        base.OnNetworkSpawn();
    }
}
