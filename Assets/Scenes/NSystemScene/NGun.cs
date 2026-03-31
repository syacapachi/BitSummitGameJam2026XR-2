using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class NGun : NetworkBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] LineRenderer laserLine;
    [SerializeField] NGunAudioObserver audioObserver;
    [SerializeField] AmmoUI ammoUI;
    [SerializeField] WeaponSettingsSO weaponSettings;
    
    float nextFire;
    private static readonly WaitForSeconds wait01 = new WaitForSeconds(0.1f);
    [SerializeField] NetworkVariable<int> syncedAmmo = new(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    private bool isReloading = false;
    public WeaponSettingsSO WeaponSettings => weaponSettings;

    public int AmmoVal => syncedAmmo.Value;

    public float reloadTime => weaponSettings.reloadTime; // AmmoUIが参照できるように
    private ICountDownUI CountDownUI => ammoUI; // ICountDownUIを実装したAmmoUIを参照
    private IProgressUI ProgressUI => ammoUI; // IProgressUIを実装したAmmoUIを参照

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
        if (IsOwner)
        {
            ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.InputReciver.OnFireed += ShootRpc;  
        }
    }
    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            syncedAmmo.Value = weaponSettings.maxAmmo;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner) 
        {
            ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot.InputReciver.OnFireed -= ShootRpc;
        }
    }
    
    public override void OnLostOwnership()
    {

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

    private void ShootRpc()
    {
        if(!ManagerLocator.Instance.AllGameManager.IsGameStart) return;
        if (isReloading) return;
        if (Time.time < nextFire) return;

        nextFire = Time.time + weaponSettings.fireRate;
        syncedAmmo.Value--;

        GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        var job = GetComponent<PlayerPropaty>().Job;


        var bullet = obj.GetComponent<NBullet>();

        bullet.shooterId = OwnerClientId;

        // ★ここでstateを設定
        bullet.state = job switch
        {
            PlayerPropaty.PlayerJob.Human => BulletState.Human,
            PlayerPropaty.PlayerJob.Ghost => BulletState.Ghost,
            _ => BulletState.Both,
        };
        var stats = GetComponent<PlayerStats>();
        stats.AddShot();
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }


    private IEnumerator Reload()
    {
        isReloading = true;
        audioObserver.PlayReloadSound();
        Debug.Log("Reloading...");
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        for (float t = 0; t < reloadTime; t += 0.1f)
        {
            ProgressUI.UpdateProgress(t/reloadTime);
            yield return wait01;
        }
        ProgressUI.UpdateProgress(0);

        syncedAmmo.Value = weaponSettings.maxAmmo;
        isReloading = false;
        CountDownUI.UpdateCount(weaponSettings.maxAmmo, weaponSettings.maxAmmo);
    }
    private void OnAmmoChanged(int oldVal, int newVal)
    {
        
        if (isReloading) return;
        if (oldVal < newVal) return;
        CountDownUI.UpdateCount(newVal, weaponSettings.maxAmmo);
        audioObserver.PlayShotSound();
        if (newVal <= 0)
        {
            StartCoroutine(Reload());
        }
    }
}