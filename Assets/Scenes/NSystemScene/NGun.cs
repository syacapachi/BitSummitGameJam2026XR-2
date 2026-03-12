using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
public class NGun : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Transform markerPoint;
    public LineRenderer laserLine;


    public WeaponSettingsSO weaponSettings;

    [SerializeField] PlayerItemControll itemControll;
    /// <summary>
    /// サーバーのみ参照を持つフィールド。プレイヤーのマーカーオブジェクトを参照するために使用される。クライアントはこのフィールドを直接参照せず、RPCを介してマーカーの位置を更新する。
    /// </summary>
    public AttachableBehaviour playerMarker;
    [SerializeField] AttachableNode node;
    PlayerControls controls;
    InputAction fireAction;
    InputAction markerAction;
    float nextFire;
    public NetworkVariable<int> syncedAmmo = new(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReloading = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    bool isMarkAttached = true;
    Coroutine markerCoroutine;
    public float reloadTime => weaponSettings.reloadTime; // AmmoUIが参照できるように

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

        if (Physics.Raycast(firePoint.position, forward, out hit, weaponSettings.laserDistance))
        {
            // ���������ꍇ
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ������ꍇ
            laserLine.SetPosition(1, firePoint.position + forward * weaponSettings.laserDistance);
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            fireAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["Fire"];
            markerAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["Marker"];

            fireAction.performed += _ => ShootRpc();
            markerAction.performed += _ => PlaceMarkerRpc();
        }
    }
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            syncedAmmo.Value = weaponSettings.maxAmmo;
            TryGetPlayerMarker();
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner) 
        {
            fireAction.performed -= _ => ShootRpc();
            markerAction.performed -= _ => PlaceMarkerRpc();
        }
        if(IsServer)
        {
            if (markerCoroutine != null)
            {
                StopCoroutine(markerCoroutine);
            }
        }
    }
    
    private bool TryGetPlayerMarker()
    {
        if (!IsServer) return false;
        if (itemControll.TryGetItem("Marker", out NetworkBehaviourReference item))
        {
            if (item.TryGet(out NetworkBehaviour behaviour))
            {
                playerMarker = behaviour as AttachableBehaviour;
                return playerMarker != null;
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
       controls?.Disable();
    }
    /*
    private void ShootRpc()
    {
        // リロード中は撃てない
        if (isReloading) return;

        // 弾がないならリロード開始
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // 連射クールダウン
        if (Time.time < nextFire) return;

        nextFire = Time.time + weaponSettings.fireRate;

        currentAmmo--;

        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        obj.GetComponent<NetworkObject>().Spawn();
    }
    */
    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        Debug.Log("ShootRpc called");
        if (isReloading.Value) return;
        if (Time.time < nextFire) return;

        nextFire = Time.time + weaponSettings.fireRate;
        syncedAmmo.Value--;

        // ① 弾を生成
        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        var job = GetComponent<PlayerPropaty>().Job;
        string layerName = job switch
        {
            PlayerPropaty.PlayerJob.Human => "Human",
            PlayerPropaty.PlayerJob.Ghost => "Ghost",
            PlayerPropaty.PlayerJob.Both => "Default",
            _ => "Default"
        };
        obj.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<NBullet>();

        bullet.shooterId = OwnerClientId;
        // ③ ネットワークでSpawn
        obj.GetComponent<NetworkObject>().Spawn();
        if (syncedAmmo.Value <= 0 && !isReloading.Value)
        {
            StartCoroutine(Reload());
            return;
        }
    }

    private IEnumerator Reload()
    {
        isReloading.Value = true;
        Debug.Log("Reloading...");
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        var wait = new WaitForSeconds(weaponSettings.reloadTime);
        yield return wait;

        syncedAmmo.Value = weaponSettings.maxAmmo;
        isReloading.Value = false;
    }

    [Rpc(SendTo.Server)]
    private void PlaceMarkerRpc()
    {
        Debug.Log("PlaceMarkerRpc called");
        if (firePoint == null) return;
        if (markerPoint == null) return;

        if (playerMarker == null && !TryGetPlayerMarker())
        {
             Debug.LogWarning("Player marker not found. Cannot place marker.");
             return;
        }
        
        RaycastHit hit;
        Vector3 forward = markerPoint.forward;

        if (Physics.Raycast(markerPoint.position, forward, out hit, weaponSettings.laserDistance))
        {
            MoveMarkerClientRpc(hit.point);
        }
        isMarkAttached = false;
    }

    [Rpc(SendTo.Server)]
    private void MoveMarkerClientRpc(Vector3 pos)
    {
        if (playerMarker == null && !TryGetPlayerMarker())
        {
            Debug.LogWarning("Player marker not found. Cannot place marker.");
            return;
        }

        if (isMarkAttached) 
        { 
            playerMarker.GetComponentInChildren<AttachableBehaviour>().Detach(); 
        }
        else
        {
            if(markerCoroutine != null)  
                StopCoroutine(markerCoroutine);
        }
        playerMarker.gameObject.transform.position = pos;
        markerCoroutine = StartCoroutine(MarkerBackCorutine());

        //var renderer = playerMarker.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
    }
    private IEnumerator MarkerBackCorutine()
    {
        yield return new WaitForSeconds(5f);
        if(playerMarker != null)
        {
            playerMarker.GetComponentInChildren<AttachableBehaviour>().Attach(node);
            playerMarker.gameObject.transform.localPosition = Vector3.zero;
            isMarkAttached = true;
        }
    }
}