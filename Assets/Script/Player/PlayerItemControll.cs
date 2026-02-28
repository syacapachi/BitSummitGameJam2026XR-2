using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.Build;
using UnityEngine;

public class PlayerItemControll : NetworkBehaviour
{
    [SerializeField] AttachableNode node;
    [SerializeField] GameObject markerPrefab;

    private readonly NetworkList<NetworkEntry> typeObjectList = new();

    [SerializeField] public int DicSize = 0;

    public bool TryGetItem(string name,out NetworkBehaviourReference instance)
    {
        foreach(NetworkEntry entry in typeObjectList)
        {
            if (entry.Key.Equals(name))
            {
                instance = entry.Reference;
                return true;
            }
        }
        instance = null;
        return false;
        
    }
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
            SpawnMarkerRpc();
    }
    [Rpc(SendTo.Server)]
    private void SpawnMarkerRpc()
    {
        var markerInstance =
            NetworkObject.InstantiateAndSpawn(markerPrefab, NetworkManager, OwnerClientId);

        var attach = markerInstance.GetComponentInChildren<AttachableBehaviour>();
        attach.Attach(node);

        
        SendMarkerReferenceClientRpc(node.NetworkObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    private void SendMarkerReferenceClientRpc(NetworkObjectReference markerRef)
    {
        if (markerRef.TryGet(out NetworkObject obj))
        {
            Debug.Log(obj.gameObject.name);
            var attach = obj.GetComponentInChildren<AttachableBehaviour>();
            NetworkEntry entry = new NetworkEntry("Marker",attach);
            typeObjectList.Add(entry);
            DicSize = typeObjectList.Count;
        }
        else
        {
            Debug.LogError("Marker is null");
        }
    }
}
