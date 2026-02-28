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

    private Dictionary<FixedString64Bytes, AttachableBehaviour> typeObjectDic = new();

    public bool TryGetItem(string name,out AttachableBehaviour instance)
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
        GameObject markerInstance = NetworkObject.InstantiateAndSpawn(markerPrefab,NetworkManager,OwnerClientId).gameObject;
        AttachableBehaviour attach = markerInstance.GetComponentInChildren<AttachableBehaviour>();
        attach.Attach(node);
        AddDictionaryRpc("Marker",attach.NetworkBehaviourId);
    }
    [Rpc(SendTo.Everyone,InvokePermission = RpcInvokePermission.Server)]
    private void AddDictionaryRpc(FixedString64Bytes name,ushort networkBehaviourId)
    {
        typeObjectDic.Add(name, (AttachableBehaviour)NetworkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId));
    }
}
