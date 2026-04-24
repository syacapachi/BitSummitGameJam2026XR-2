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
            blinkEffect = marker.GetComponentInChildren<MarkerBlinkEffect>(); // 水野が追加した
            attach = marker.GetComponentInChildren<AttachableBehaviour>();
            networkObject.SpawnWithOwnership(OwnerClientId);
            isMarkAttachedServerOnly = false;
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

        // 水野が追加した
        if (blinkEffect != null)
        {
            blinkEffect.StartBlink();
        }
        // 以上

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
            isMarkAttachedServerOnly = true;

            // 水野が追加した
            if (blinkEffect != null)
            {
                blinkEffect.StopBlink();
            }
            // 水野が追加した

        }
        else
        {
            Debug.LogError($"[{gameObject.name}]AttachableBehaviour is null");
            isMarkAttachedServerOnly = false;
        }
        
    }
}
