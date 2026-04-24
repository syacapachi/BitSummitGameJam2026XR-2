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
    [SerializeField] int laserDistance = 50;
    [SerializeField] MarkerAudioController markerAudioController;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent markerEvent;
    AttachableBehaviour attach;
    //水野が追加した。マーカーの発光用
    MarkerBlinkEffect blinkEffect;
    //以上
    bool isMarkAttachedServerOnly = false;
    Coroutine markerCoroutine;
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        markerEvent.Register(PlaceMarkerRpc);
    }
  
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            var marker = GameObject.Instantiate(playerMarker);
            var networkObject = marker.GetComponent<NetworkObject>();
            
            attach = marker.GetComponentInChildren<AttachableBehaviour>();
            networkObject.SpawnWithOwnership(OwnerClientId);
            GetBlincEffect(networkObject);
            isMarkAttachedServerOnly = false;
        }
        
    }
    private void GetBlincEffect(NetworkObjectReference reference)
    {
        if(reference.TryGet(out var networkObject))
        {
            blinkEffect = networkObject.GetComponentInChildren<MarkerBlinkEffect>(); // 水野が追加した
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
            markerEvent.Unregister(PlaceMarkerRpc);
        }
    }
    private void Update()
    {
        if (!IsOwner) return;
        UpdateLaser();
    }
    void UpdateLaser()
    {
        if (lineRenderer == null || firePoint == null) return;

        // �J�n�_
        lineRenderer.SetPosition(0, firePoint.position);

        // Raycast �Œ��e�_�𔻒�
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out RaycastHit hit, laserDistance))
        {
            // ���������ꍇ
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ������ꍇ
            lineRenderer.SetPosition(1, firePoint.position + forward * laserDistance);
        }
    }
    [Rpc(SendTo.Server)]
    private void PlaceMarkerRpc()
    {
        if (firePoint == null) return;

        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out RaycastHit hit, laserDistance))
        {
            MoveMarkerServerRpc(hit.point);
            markerAudioController.OnMarkerSondPlayRpc(hit.point);
        }
    }

    [Rpc(SendTo.Server)]
    private void MoveMarkerServerRpc(Vector3 pos)
    {
        if (attach == null)
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.");
            return;
        }
        if (isMarkAttachedServerOnly)
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

        StartBlinkRpc();

        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if (attach != null)
        {
            attach.Attach(node);
            attach.gameObject.transform.localPosition = Vector3.zero;
            isMarkAttachedServerOnly = true;

            StopBlinkRpc();

        }
        else
        {
            Debug.LogError($"[{gameObject.name}]AttachableBehaviour is null");
            isMarkAttachedServerOnly = false;
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void StartBlinkRpc()
    {
        // 水野が追加した
        if (blinkEffect != null)
        {
            blinkEffect.StartBlink();
        }
        // 水野が追加した
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void StopBlinkRpc()
    {
        // 水野が追加した
        if (blinkEffect != null)
        {
            blinkEffect.StopBlink();
        }
        // 水野が追加した
    }
}
