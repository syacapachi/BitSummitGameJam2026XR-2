using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarkerController : NetworkBehaviour
{
    [SerializeField] Transform firePoint;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject playerMarker;
    [SerializeField] AttachableNode node;
    [SerializeField] int laserDistance = 10;
    [SerializeField] MarkerAudioController markerAudioController;
    GameObject marker;
    InputAction markerAction;
    bool isMarkAttached = true;
    Coroutine markerCoroutine;
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        markerAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["Marker"];
        markerAction.performed += _ => PlaceMarkerRpc();
    }
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            marker = GameObject.Instantiate(playerMarker);
            var networkObject = marker.GetComponent<NetworkObject>();
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
        if (IsOwner)
        {
            markerAction.performed -= _ => PlaceMarkerRpc();
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
            markerAudioController.OnMarkerSondPlayRpc(hit.point);
        }
        isMarkAttached = false;
    }
    
    [Rpc(SendTo.Server)]
    private void MoveMarkerClientRpc(Vector3 pos)
    {
        if (marker == null)
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.");
            return;
        }
        if (isMarkAttached)
        {
            marker.GetComponentInChildren<AttachableBehaviour>().Detach();
        }
        else
        {
            if (markerCoroutine != null)
                StopCoroutine(markerCoroutine);
        }
        marker.transform.position = pos;
        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if (marker != null)
        {
            marker.GetComponentInChildren<AttachableBehaviour>().Attach(node);
            marker.transform.localPosition = Vector3.zero;
            isMarkAttached = true;
        }
    }
}
