using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AttachableRigidBody : AttachableBehaviour
{
    [SerializeField] Rigidbody body;

    public void Focus()
    {
        NetworkObject owner = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.NetworkObject;
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
            if (netObject.TryGetComponent<PlayerRoot>(out var root))
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
}
