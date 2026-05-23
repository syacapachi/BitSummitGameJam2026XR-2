using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR;

public class MarkerController : NetworkBehaviour
{
    [Header("Fps")]
    [SerializeField] Transform playerHead;
    [Header("XR")]
    [SerializeField] Transform firePoint;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject playerMarker;
    [SerializeField] AttachableNode node;
    [SerializeField] LayerMask markerHitLayerMask;
    [SerializeField] int laserDistance = 50;
    [SerializeField] float markerBackTime = 5f;
    [SerializeField] MarkerAudioController markerAudioController;
    [Header("HasMarkerCharge")]
    [SerializeField] bool hasMarkerChargeTimeServerOnly = false;
    [Header("Publish Event")]
    [SerializeField] ULongEvent MarkerPlaceEventServerOnly;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent markerEvent;
    AttachableBehaviour attachServerOnly;
    //水野が追加した。マーカーの発光用サーバーだけもつ
    MarkerBlinkEffect blinkEffectServerOnly;
    //以上
    bool isMarkAttachedServerOnly = false;
    Coroutine markerCoroutine;
    private static WaitForSeconds wait;

    public Transform FirePoint
    {
        get
        {
            if (!XRSettings.isDeviceActive)
            {
                return playerHead;
            }
            return firePoint;
        }
    }

    private void Awake()
    {
        wait = new WaitForSeconds(markerBackTime);
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            lineRenderer.enabled = false;
            return;
        }
        markerEvent.Register(PlaceMarkerWhenEnabled);
        StartCoroutine(LaserUpdateCoroutine());
        lineRenderer.enabled = XRSettings.isDeviceActive;
    }

    protected override void OnNetworkPostSpawn()
    {
        //Debug.Log($"{nameof(MarkerController)},IsOwner={IsOwner},Owned by={OwnerClientId}");
        //IsServerだと、クライアントでスポーンする前にサーバーでスポーンする->オーナー権限が消えるため、IsOwnerでスポーンさせる
        if (IsOwner)
        {
            //オーナーがSpawnした後でRPCを呼び出す
            CreateMerkerRpc();
        }
    }
    [Rpc(SendTo.Server)]
    private void CreateMerkerRpc()
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
            markerEvent.Unregister(PlaceMarkerWhenEnabled);
        }
    }
    //オーナー以外で毎フレームチェックさせるオーバーヘッドをなくすためコルーチン化
    IEnumerator LaserUpdateCoroutine()
    {
        while (IsOwner)
        {
            UpdateLaser();
            yield return null;
        }
    }

    void UpdateLaser()
    {
        if (lineRenderer == null || FirePoint == null) return;

        // �J�n�_
        lineRenderer.SetPosition(0, FirePoint.position);

        // Raycast �Œ��e�_�𔻒�
        Vector3 forward = FirePoint.forward;

        if (Physics.Raycast(FirePoint.position, forward, out RaycastHit hit, laserDistance))
        {
            // ���������ꍇ
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ������ꍇ
            lineRenderer.SetPosition(1, FirePoint.position + forward * laserDistance);
        }
    }
    /// <summary>
    /// 一度おいてから、一定時間が立たないと移動できません
    /// </summary>
    private void PlaceMarkerWhenEnabled()
    {
        PlaceMarkerRpc();
    }
    [Rpc(SendTo.Server)]
    private void PlaceMarkerRpc()
    {
        if (ManagerLocator.Instance == null
            || ManagerLocator.Instance.GameStateManager == null
            || !ManagerLocator.Instance.GameStateManager.IsGamePlaying
            ) return;
        if (FirePoint == null) return;

        Vector3 forward = FirePoint.forward;

        if (Physics.Raycast(FirePoint.position, forward, out RaycastHit hit, laserDistance, markerHitLayerMask))
        {
            MoveMarkerServerOnly(hit.point);
        }
    }
    private void MoveMarkerServerOnly(Vector3 pos)
    {
        if (attachServerOnly == null)
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.", gameObject);
            return;
        }
        //マーカーが出てない
        if (isMarkAttachedServerOnly)
        {
            //マーカーをプレーヤーから引き剝がす。(基本はプレーヤーにくっついて、見えない)
            attachServerOnly.Detach();
            isMarkAttachedServerOnly = false;
        }
        //マーカー出てる & 出てる間移動不可
        else if (hasMarkerChargeTimeServerOnly)
        {
            return;
        }
        else
        {
            //マーカーの消失コルーチンをリセット
            if (markerCoroutine != null)
            {
                StopCoroutine(markerCoroutine);
                markerCoroutine = null;
            }
        }
        //マーカーの位置を更新
        attachServerOnly.gameObject.transform.position = pos;

        // 水野が追加した 非サーバーに点滅を指示
        if (blinkEffectServerOnly != null)
        {
            blinkEffectServerOnly.StartBlinkRpc();
        }
        // 水野が追加した

        markerCoroutine = StartCoroutine(MarkerBackCorutine());
        markerAudioController.OnMarkerSondPlayRpc(pos);
        MarkerPlaceEventServerOnly.Invoke(OwnerClientId);
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return wait;
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
            Debug.LogError($"[{gameObject.name}]AttachableBehaviour is null", gameObject);
            isMarkAttachedServerOnly = false;
        }
    }
}
