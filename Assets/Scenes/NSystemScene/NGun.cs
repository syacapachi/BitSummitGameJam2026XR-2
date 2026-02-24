using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using Unity.Netcode;

public class NGun : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public LineRenderer laserLine;
    public float laserDistance = 50f;

    public float fireRate = 0.2f;

    public GameObject markerPrefab;

    PlayerControls controls;
    float nextFire;

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
        controls.Player.Marker.performed += ctx => PlaceMarker();
        
        controls.Enable();
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

    void PlaceMarker()
    {
        if (markerPrefab == null || firePoint == null) return;

        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            // �}�[�J�[Prefab�𐶐�
            GameObject markerObj = Instantiate(markerPrefab, hit.point, Quaternion.identity);

            // Marker.cs �����Ă���ΐF������Ȃǂ̐ݒ�͎����ōs����
            Marker markerScript = markerObj.GetComponent<Marker>();
            if (markerScript != null)
            {
                markerScript.color = Color.red;   // �D���ȐF�ɕύX�\
                markerScript.lifeTime = 5f;      // ������܂ł̎���
            }
        }
    }
}