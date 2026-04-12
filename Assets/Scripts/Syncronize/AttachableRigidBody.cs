using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AttachableRigidBody : AttachableBehaviour
{
    [SerializeField] Rigidbody body;
    private XRGrabInteractable xRGrabInteractable;
    protected override void Awake()
    {
        base.Awake();
        if (TryGetComponent<XRGrabInteractable>(out var interacter))
        {
            xRGrabInteractable = interacter;
            interacter.selectEntered.AddListener(args => OnSelect());
            interacter.selectExited.AddListener(args => OnSelectExit());
        }
    }
    public void OnSelect()
    {
        NetworkObject owner = ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.NetworkObject;
        AttachRpc(owner);
    }
    public void OnSelectExit()
    {
        DetachRpc();
    }
    [Rpc(SendTo.Server)]
    private void AttachRpc(NetworkObjectReference reference)
    {
        if(reference.TryGet(out var netObject))
        {
            if (netObject.TryGetComponent<NetworkPlayerRoot>(out var root))
            {
                Attach(root.itemControll.Node);
                //NetworkObject.ChangeOwnership(netObject.OwnerClientId);
                this.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}][AttachRpc] Didnt get PlayerRoot at[{netObject.name}]");
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}][AttachRpc] Didnt get NetworkObject at[{reference.NetworkObjectId}]");
        }
    }
    [Rpc(SendTo.Server)]
    private void DetachRpc()
    {
        Vector3 pos = transform.position;
        Detach();
        //NetworkObject.ChangeOwnership(0);
        transform.position = pos;
    }
    protected override void OnAttachStateChanged(AttachState attachState, AttachableNode attachableNode)
    {
        if (body == null) return;
        switch (attachState)
        {
            case AttachState.Attached:
                break;
            case AttachState.Detached:
                body.isKinematic = false;
                break;
            case AttachState.Attaching:
                body.isKinematic = true;
                break;
            case AttachState.Detaching:
                break;
        }
    }
    private void Reset()
    {
        if (TryGetComponent<XRGrabInteractable>(out var interacter))
        {
            xRGrabInteractable = interacter;
        }
    }
}
