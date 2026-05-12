using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerItemControll : NetworkBehaviour
{
    [SerializeField] AttachableNode node;

    //private readonly NetworkList<NetworkEntry> serverTypeObjectList = new();
    //public event Action<string, NetworkBehaviourReference> OnItemAdded;
    public AttachableNode Node => node;
    //public bool TryGetItem(string name,out NetworkBehaviourReference instance)
    //{
    //    foreach(NetworkEntry entry in serverTypeObjectList)
    //    {
    //        if (entry.Key.Equals(name))
    //        {
    //            instance = entry.Reference;
    //            return true;
    //        }
    //    }
    //    instance = null;
    //    return false;
        
    //}
    //public override void OnNetworkSpawn()
    //{
    //    serverTypeObjectList.OnListChanged += OnListChangedHandle;
    //    if (!IsOwner) return;
    //        SpawnMarkerRpc();
    //}
    //private void OnListChangedHandle(NetworkListEvent<NetworkEntry> changeEvent)
    //{
    //    Debug.Log($"List changed: {changeEvent.Type}");
    //    switch(changeEvent.Type)
    //    {
    //        case NetworkListEvent<NetworkEntry>.EventType.Add : 
    //            Debug.Log($"Added entry: {changeEvent.Value.Key}");
    //            OnItemAdded?.Invoke(changeEvent.Value.Key.ToString(), changeEvent.Value.Reference);
    //            break;
    //        case NetworkListEvent<NetworkEntry>.EventType.Remove : 
    //            Debug.Log($"Removed entry: {changeEvent.Value.Key}"); 
    //            break;
    //        case NetworkListEvent<NetworkEntry>.EventType.Value : 
    //            Debug.Log($"Updated entry: {changeEvent.Value.Key}");
    //            break;

    //        default : 
    //            Debug.Log("Unknown change type");
    //            break;
    //    }
    //}
    //[Rpc(SendTo.Server)]
    //private void SpawnMarkerRpc()
    //{
    //    var markerInstance =
    //        NetworkObject.InstantiateAndSpawn(markerPrefab, NetworkManager, OwnerClientId);

    //    markerInstance.gameObject.name = $"Marker_{OwnerClientId}";

    //    var attach = markerInstance.GetComponentInChildren<AttachableBehaviour>();
    //    attach.Attach(node);


    //    NetworkEntry entry = new NetworkEntry("Marker", attach);
    //    serverTypeObjectList.Add(entry);
    //}
}
