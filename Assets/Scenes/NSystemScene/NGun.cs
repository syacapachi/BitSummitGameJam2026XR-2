using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using Unity.Netcode;
using Unity.Netcode.Components;

public class NGun : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public LineRenderer laserLine;
    public float laserDistance = 50f;

    public float fireRate = 0.2f;
    [SerializeField] PlayerItemControll itemControll;
    public Transform playerMarker;

    PlayerControls controls;
    float nextFire;

    bool isMarkAttached = true;

    void Update()
    {
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
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        controls = new PlayerControls();
        controls.Player.Fire.performed += ctx => ShootRpc();
        controls.Player.Marker.performed += ctx => PlaceMarkerRpc();
        
        controls.Enable();
        if(itemControll.TryGetItem("Marker",out GameObject item))
        {
            playerMarker = item.transform;
        }
    }
    public override void OnNetworkDespawn()
    {
        controls.Disable();
    }

    [Rpc(SendTo.Server)]
    void ShootRpc()
    {
        if (Time.time < nextFire) return;

        nextFire = Time.time + fireRate;

        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        obj.GetComponent<NetworkObject>().Spawn();
    }

    [Rpc(SendTo.Server)]
    void PlaceMarkerRpc()
    {
        if (playerMarker == null || firePoint == null) return;

        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            MoveMarkerClientRpc(hit.point);
        }
        isMarkAttached = false;
    }

    [Rpc(SendTo.Everyone)]
    void MoveMarkerClientRpc(Vector3 pos)
    {
        if (!IsOwner) return;   // ★これ

        if(isMarkAttached) playerMarker.GetComponentInChildren<AttachableBehaviour>().Detach();
        playerMarker.position = pos;
        
        var renderer = playerMarker.GetComponent<MeshRenderer>();
        renderer.enabled = true;
    }
}