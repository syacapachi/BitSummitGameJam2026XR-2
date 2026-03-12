using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public LineRenderer laserLine;
    public WeaponSettingsSO weaponSettings;
    private float nextFire;
    [SerializeField] AudioSource allShootSoundSource;
    public NetworkVariable<int> syncedAmmo = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isReloading = false;
    
    public float reloadTime => weaponSettings.reloadTime; // AmmoUIが参照できるように

    private void OnEnable()
    {
        syncedAmmo.OnValueChanged += OnAmmoChanged;
    }
    private void OnDisable()
    {
        syncedAmmo.OnValueChanged -= OnAmmoChanged;
    }

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
    }
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            syncedAmmo.Value = weaponSettings.maxAmmo;
        }
    }
    public void Activate()
    {
        ShootRpc();
    }
    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        Debug.Log("ShootRpc called");
        if (isReloading) return;
        if (Time.time < nextFire) return;

        nextFire = Time.time + weaponSettings.fireRate;
        syncedAmmo.Value--;

        // ① 弾を生成
        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        var job = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.propaty.Job;
        string layerName = PlayerPropaty.jobToLayerDic[job];
        obj.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<NBullet>();

        bullet.shooterId = OwnerClientId;
        // ③ ネットワークでSpawn
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        var wait = new WaitForSeconds(weaponSettings.reloadTime);
        yield return wait;

        syncedAmmo.Value = weaponSettings.maxAmmo;
        isReloading = false;
    }
    private void OnAmmoChanged(int oldestAmmo, int newestAmmo)
    {
        allShootSoundSource?.Play();
        if (newestAmmo <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
            return;
        }
    }
}
