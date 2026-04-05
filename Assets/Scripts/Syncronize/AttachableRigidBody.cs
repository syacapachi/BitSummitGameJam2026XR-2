using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AttachableRigidBody : AttachableBehaviour
{
    [SerializeField] Rigidbody body;
    public void Focus()
    {
        NetworkObject owner = ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.NetworkObject;
        AttachRpc(owner);
    }
    public void UnFocus()
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
        var interacter = GetComponent<XRGrabInteractable>();
        if (interacter != null)
        {
            interacter.focusEntered.AddListener(args => Focus());
            interacter.focusExited.AddListener(args => UnFocus());
        }
    }
}
