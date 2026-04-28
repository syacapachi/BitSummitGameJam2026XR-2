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
    AttachableBehaviour attachServerOnly;
    //水野が追加した。マーカーの発光用サーバーだけもつ
    MarkerBlinkEffect blinkEffectServerOnly;
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
        Debug.Log($"{nameof(MarkerController)},IsOwner={IsOwner},Owned by={OwnerClientId}");
        //IsServerだと、クライアントでスポーンする前にサーバーでスポーンする->オーナー権限が消えるため、IsOwnerでスポーンさせる
        if (IsOwner)
        {
            //オーナーがSpawnした後でRPCを呼び出す
            CreateMerkerRpc();
        }
    }
    [Rpc(SendTo.Server)]
    private  void CreateMerkerRpc()
    {
        var marker = GameObject.Instantiate(playerMarker);
        var networkObject = marker.GetComponent<NetworkObject>();
        attachServerOnly = marker.GetComponentInChildren<AttachableBehaviour>();
        networkObject.SpawnWithOwnership(OwnerClientId);
        blinkEffectServerOnly = networkObject.GetComponentInChildren<MarkerBlinkEffect>();
        attachServerOnly.Attach(node);
        isMarkAttachedServerOnly = true;
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
        if (attachServerOnly == null)
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.");
            return;
        }
        if (isMarkAttachedServerOnly)
        {
            attachServerOnly.Detach();
            isMarkAttachedServerOnly = false;
        }
        else
        {
            if (markerCoroutine != null) 
            {
                StopCoroutine(markerCoroutine);
                markerCoroutine = null;
            }
        }
        attachServerOnly.gameObject.transform.position = pos;

        // 水野が追加した 非サーバーに点滅を指示
        if (blinkEffectServerOnly != null)
        {
            blinkEffectServerOnly.StartBlinkRpc();
        }
        // 水野が追加した

        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if (attachServerOnly != null)
        {
            attachServerOnly.Attach(node);
            attachServerOnly.gameObject.transform.localPosition = Vector3.zero;
            isMarkAttachedServerOnly = true;

            // 水野が追加した
            if (blinkEffectServerOnly != null)
            {
                blinkEffectServerOnly.StopBlinkRpc();
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
