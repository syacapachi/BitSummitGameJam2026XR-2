using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class MarkerController : NetworkBehaviour
{
    [SerializeField] Transform firePoint;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject playerMarker;
    [SerializeField] AttachableNode node;
    [SerializeField] int laserDistance = 10;
    bool isMarkAttached = true;
    Coroutine markerCoroutine;
    
    
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            playerMarker = GameObject.Instantiate(playerMarker);
            var networkObject = playerMarker.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(OwnerClientId);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (markerCoroutine != null)
            {
                StopCoroutine(markerCoroutine);
            }
        }
    }
    [Rpc(SendTo.Server)]
    private void PlaceMarkerRpc()
    {
        Debug.Log("PlaceMarkerRpc called");
        if (firePoint == null) return;

        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            MoveMarkerClientRpc(hit.point);
        }
        isMarkAttached = false;
    }
    
    [Rpc(SendTo.Server)]
    private void MoveMarkerClientRpc(Vector3 pos)
    {
        if (isMarkAttached)
        {
            playerMarker.GetComponentInChildren<AttachableBehaviour>().Detach();
        }
        else
        {
            if (markerCoroutine != null)
                StopCoroutine(markerCoroutine);
        }
        playerMarker.transform.position = pos;
        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if (playerMarker != null)
        {
            playerMarker.GetComponentInChildren<AttachableBehaviour>().Attach(node);
            playerMarker.transform.localPosition = Vector3.zero;
            isMarkAttached = true;
        }
    }
}
