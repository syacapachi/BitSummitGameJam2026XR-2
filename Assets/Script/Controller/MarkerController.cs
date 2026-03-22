using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.Experimental.GraphView;
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
    AttachableBehaviour attach;
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
            var marker = GameObject.Instantiate(playerMarker);
            var networkObject = marker.GetComponent<NetworkObject>();
            attach = marker.GetComponentInChildren<AttachableBehaviour>();
            networkObject.SpawnWithOwnership(OwnerClientId);
            isMarkAttached = false;
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
        if(!ManagerLocator.Instance.AllGameManager.IsGameStart) return;
        if (firePoint == null) return;

        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            MoveMarkerClientRpc(hit.point);
            markerAudioController.OnMarkerSondPlayRpc(hit.point);
        }
    }
    
    [Rpc(SendTo.Server)]
    private void MoveMarkerClientRpc(Vector3 pos)
    {
        if (attach == null)
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.");
            return;
        }
        if (isMarkAttached)
        {
            attach.Detach();
        }
        else
        {
            if (markerCoroutine != null) 
            {
                StopCoroutine(markerCoroutine);
                markerCoroutine = null;
            }
        }
        attach.gameObject.transform.position = pos;
        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
;
        if (attach != null)
        {
            attach.Attach(node);
            attach.gameObject.transform.localPosition = Vector3.zero;
            isMarkAttached = true;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}]AttachableBehaviour is null");
            isMarkAttached = false;
        }
        
    }
}
