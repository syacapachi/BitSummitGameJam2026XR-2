using UnityEngine;
using UnityEngine.XR;
public class NGun : GunController
{
    [Header("Fps")]
    [SerializeField] Transform playerHead;
    [Header("gun")]
    [SerializeField] LineRenderer laserLine;
    [SerializeField] NGunAudioObserver audioObserver;
    [SerializeField] AmmoUI ammoUI;
    [SerializeField] PlayerStats playerStats;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent fireEvent;
    [SerializeField] GameEffectDataEvent networkEvent;

    protected override IResultCollector Collector => playerStats;
    public override Transform FirePoint 
    {
        get
        {
            if (!XRSettings.isDeviceActive)
            {
                return playerHead;
            }
            return base.FirePoint;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            fireEvent.Register(base.Activate);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner) 
        {
            fireEvent.Unregister(base.Activate);
        }
    }
    void Update()
    {
        if (!IsOwner) return;

        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (laserLine == null || FirePoint == null) return;

        // �J�n�_
        laserLine.SetPosition(0, FirePoint.position);

        // Raycast �Œ��e�_�𔻒�
        Vector3 forward = FirePoint.forward;

        if (Physics.Raycast(FirePoint.position, forward, out RaycastHit hit, WeaponSettings.laserDistance))
        {
            // ���������ꍇ
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ������ꍇ
            laserLine.SetPosition(1, FirePoint.position + forward * WeaponSettings.laserDistance);
        }
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
    /*
    private void ShootRpc()
    {
        Debug.Log("ShootRpc called");
        if (isReloading) return;
        if (Time.time < nextFire) return;

        nextFire = Time.time + weaponSettings.fireRate;
        syncedAmmo.Value--;

        // ① 弾を生成
        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        //NetworkObject obj = ManagerLocator.Instance.AllObjectPool.GetNetworkObject(bulletPrefab);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        //GameObject go = obj.gameObject;
        var job = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.propaty.Job;
        string layerName = PlayerPropaty.jobToLayerDic[job];
        obj.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<NBullet>();

        bullet.shooterId = OwnerClientId;
        // ③ ネットワークでSpawn
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }
    */

    protected override void OnShootServer()
    {
        base.OnShootServer();
        if (playerStats != null)
            playerStats.AddShot();
    }
    public override void UpdateProgress(float progress)
    {
        if (ammoUI == null) return;
        ammoUI.UpdateProgress(progress);
    }
    public override void UpdateCount(int remainVal, int maxVal)
    {
        if (ammoUI == null) return;
        ammoUI.UpdateCount(remainVal, maxVal);
    }
    public override void PlayReloadSound()
    {
        audioObserver.PlayReloadSound();
    }
    public override void PlayShotSound()
    {
        audioObserver.PlayShotSound();
    }
}