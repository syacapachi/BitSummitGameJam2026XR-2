using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.Build;
using UnityEngine;

public class PlayerItemControll : NetworkBehaviour
{
    [SerializeField] AttachableNode node;
    [SerializeField] GameObject markerPrefab;

    private Dictionary<string,GameObject> typeObjectDic = new();

    public bool TryGetItem(string name,out GameObject instance)
    {
        return typeObjectDic.TryGetValue(name,out instance);
        
    }
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
            SpawnMarkerRpc();
    }
    [Rpc(SendTo.Server)]
    private void SpawnMarkerRpc()
    {
        GameObject markerInstance = NetworkObject.InstantiateAndSpawn(markerPrefab,NetworkManager.Singleton,OwnerClientId).gameObject;
        AttachableBehaviour attach = markerInstance.GetComponentInChildren<AttachableBehaviour>();
        attach.Attach(node);
        typeObjectDic.Add("Marker",attach.gameObject);
    }
}
