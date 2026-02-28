using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
public class NGun : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public LineRenderer laserLine;
    public float laserDistance = 50f;

    public float fireRate = 0.2f;
    [SerializeField] PlayerItemControll itemControll;
    /// <summary>
    /// サーバーのみ参照を持つフィールド。プレイヤーのマーカーオブジェクトを参照するために使用される。クライアントはこのフィールドを直接参照せず、RPCを介してマーカーの位置を更新する。
    /// </summary>
    public Transform playerMarker;
    AttachableNode node;
    PlayerControls controls;
    float nextFire;

    bool isMarkAttached = true;
    Coroutine markerCoroutine;

    void Update()
    {
        if (!IsOwner) return;

        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (laserLine == null || firePoint == null) return;

        // �J�n�_
        laserLine.SetPosition(0, firePoint.position);

        // Raycast �Œ��e�_�𔻒�
        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            // ���������ꍇ
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ������ꍇ
            laserLine.SetPosition(1, firePoint.position + forward * laserDistance);
        }
    }
    protected override void OnNetworkPostSpawn()
    {
        
        if (IsServer) 
        {
            TryGetPlayerMarker();
        }
        

        if (IsOwner)
        {
            controls = new PlayerControls();
            controls.Player.Fire.performed += ctx => ShootRpc();
            controls.Player.Marker.performed += ctx => PlaceMarkerRpc();

            controls.Enable();
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner) 
        {
            controls.Disable();
        }
            
    }
    private bool TryGetPlayerMarker()
    {
        if (!IsServer) return false;
        if (itemControll.TryGetItem("Marker", out NetworkBehaviourReference item))
        {
            if (item.TryGet(out NetworkBehaviour obj))
            {
                playerMarker = obj.gameObject.transform;
                node = obj.GetComponentInParent<AttachableNode>();
                return true;
            }
            else
            {
                Debug.LogWarning("NetworkBehaviour item not found in PlayerItemControll.");
                return false;
            }
        }
        else
        {
            Debug.LogWarning("Marker item not found in PlayerItemControll.");
            return false;
        }
    }
    public override void OnLostOwnership()
    {
       controls.Disable();
    }

    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        if (Time.time < nextFire) return;

        nextFire = Time.time + fireRate;

        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        obj.GetComponent<NetworkObject>().Spawn();
    }

    [Rpc(SendTo.Server)]
    private void PlaceMarkerRpc()
    {
        if (firePoint == null) return;

        if (playerMarker == null)
        {
            if(!TryGetPlayerMarker())
             {
                 Debug.LogWarning("Player marker not found. Cannot place marker.");
                 return;
            }
        }
        

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
            StopCoroutine(markerCoroutine);
        }
            playerMarker.position = pos;
        markerCoroutine =  StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if(playerMarker != null)
        {
            playerMarker.GetComponentInChildren<AttachableBehaviour>().Attach(node);
            playerMarker.localPosition = Vector3.zero;
            isMarkAttached = true;
        }
    }
}