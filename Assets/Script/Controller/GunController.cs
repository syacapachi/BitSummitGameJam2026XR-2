using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] WeaponSettingsSO weaponSettings;
    [SerializeField] AudioSource allShootSoundSource;
    [SerializeField] AudioSource allReloadSoundSource;
    public GameObject BulletPrefab => bulletPrefab; // NBulletから参照できるように
    public Transform FirePoint => firePoint; // NBulletから参照できるように
    public WeaponSettingsSO WeaponSettings => weaponSettings; // NBulletから参照できるように
    public NetworkVariable<int> syncedAmmo = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public int CurrentAmmo => syncedAmmo.Value; // AmmoUIが参照できるように
    public int MaxAmmo => weaponSettings.maxAmmo; // AmmoUIが参照できるように
    public float ReloadTime => weaponSettings.reloadTime; // AmmoUIが参照できるように
    /// <summary>
    /// virtualなUIフィールド。GunControllerを継承したクラスで、CountDownUIやProgressUIを実装したクラスを受け入れるためのもの。
    /// </summary>
    protected virtual ICountDownUI CountDownUI => null; // CountDownUIを実装したものを受け入れるフィールド
    protected virtual IProgressUI ProgressUI => null; // IProgressUIを実装したものを受け入れるフィールド

    protected virtual IShotSound ShotSound => null; // IShotSoundを実装したものを受け入れるフィールド
    protected virtual IReloadSound ReloadSound => null; // IReloadSoundを実装したものを受け入れるフィールド

    private static readonly WaitForSeconds wait01 = new WaitForSeconds(0.1f);
    private float nextFire;
    //こいつは、残段数のみを同期してればわかる
    private bool isReloading = false;
    private IReadOnlyDictionary<PlayerJob, PlayerLayerSettings> jobToLayerMaskDic = new Dictionary<PlayerJob, PlayerLayerSettings>();


    private void Start()
    {
        var jobManager = ManagerLocator.Instance.JobManager;
        if (jobManager == null)
        {
            Debug.LogError("PlayerJobManager not found in the scene.");
            return;
        }
        jobToLayerMaskDic = jobManager.JobLayerMaskDic;
    }
    private void OnEnable()
    {
        syncedAmmo.OnValueChanged += OnAmmoChanged;
    }
    private void OnDisable()
    {
        syncedAmmo.OnValueChanged -= OnAmmoChanged;
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
    /// <summary>
    /// 打てるかを確認
    /// </summary>
    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        if (isReloading) return;
        if (Time.time < nextFire) return;
        OnShoot();
    }
    /// <summary>
    /// 銃を撃つ具体的な処理。サーバーでのみ呼ばれる。
    /// </summary>
    protected virtual void OnShoot()
    {
        nextFire = Time.time + weaponSettings.fireInterval;
        syncedAmmo.Value--;

        // ① 弾を生成
        //GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        NetworkObject obj = ManagerLocator.Instance.AllNetworkObjectPool.GetNetworkObject(bulletPrefab,firePoint.position,firePoint.rotation);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        GameObject go = obj.gameObject;
        var job = ManagerLocator.Instance.AllPlayerManager.NetworkOwnerPlayer.propaty.Job;
        var layerName = jobToLayerMaskDic[job];
        //go.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<NBullet>();

        bullet.BulletInit(OwnerClientId, job);
        // ③ ネットワークでSpawn
        obj.SpawnWithOwnership(OwnerClientId);
    }
    private IEnumerator Reload()
    {
        isReloading = true;
        if (IsClient)
        {
            if (allReloadSoundSource != null)
            {
                allReloadSoundSource.Play();
            }
            ReloadSound?.PlayReloadSound();
        }
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        for (float t = 0; t < ReloadTime; t += 0.1f)
        {
            ProgressUI?.UpdateProgress(t / ReloadTime);
            yield return wait01;
        }
        ProgressUI?.UpdateProgress(0);

        syncedAmmo.Value = weaponSettings.maxAmmo;
        isReloading = false;
        CountDownUI?.UpdateCount(weaponSettings.maxAmmo, weaponSettings.maxAmmo);

        if (IsServer)
        {
            syncedAmmo.Value = weaponSettings.maxAmmo;
        }
        isReloading = false;
    }
    /// <summary>
    /// 残段数の変更を購読するため、サーバー・クライアントで呼ばれる
    /// </summary>
    /// <param name="oldVal"></param>
    /// <param name="newVal"></param>
    private void OnAmmoChanged(int oldVal, int newVal)
    {
        if(isReloading) return;
        if(oldVal < newVal) return;
        CountDownUI?.UpdateCount(newVal, MaxAmmo);
        if (IsClient)
        {
            if (allReloadSoundSource != null)
            {
                allShootSoundSource.Play();
            }
            ShotSound?.PlayShotSound();
        }
        
        if (newVal <= 0)
        {
            StartCoroutine(Reload());
        }
    }
}
