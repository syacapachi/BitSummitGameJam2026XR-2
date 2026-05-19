using Syacapachi.Attribute;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

public class GunController : NetworkBehaviour, ICountDownUI, IProgressUI, IShotSound, IReloadSound,IGun
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] WeaponSettingsSO weaponSettings;
    public GameObject BulletPrefab => bulletPrefab; // NBulletから参照できるように
    public virtual Transform FirePoint => firePoint; // NBulletから参照できるように
    public WeaponSettingsSO WeaponSettings => weaponSettings; // NBulletから参照できるように
    public NetworkVariable<int> syncedAmmo = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public int CurrentAmmo => syncedAmmo.Value; // AmmoUIが参照できるように
    public int MaxAmmo => weaponSettings.maxAmmo; // AmmoUIが参照できるように
    public float ReloadTime => weaponSettings.reloadTime; // AmmoUIが参照できるように

    protected virtual IResultCollector Collector { get; } = null;


    private static readonly WaitForSeconds wait01 = new WaitForSeconds(0.1f);
    private float nextFire;
    //こいつは、残段数のみを同期してればわかる
    private bool isReloading = false;
    private NetworkPlayerPropaty Propaty;

    private void OnEnable()
    {
        syncedAmmo.OnValueChanged += OnAmmoChanged;
    }
    private void OnDisable()
    {
        syncedAmmo.OnValueChanged -= OnAmmoChanged;
    }

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            Propaty = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponentInChildren<NetworkPlayerPropaty>();
            syncedAmmo.Value = weaponSettings.maxAmmo;
        }
    }
    public virtual void Activate()
    {
        if (ManagerLocator.Instance == null
            || ManagerLocator.Instance.AllGameManager == null
            || !ManagerLocator.Instance.AllGameManager.IsGamePlaying
            ) return;
        ShootRpc();
    }
    /// <summary>
    /// 打てるかを確認
    /// </summary>
    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        if (isReloading) {
            if (!IsOwner && IsClient)
            {
                PlayCantSound();
            }
            return;
        }
        if (Time.time < nextFire) return;
        OnShootServer();
    }
    /// <summary>
    /// 銃を撃つ具体的な処理。サーバーでのみ呼ばれる。
    /// </summary>
    protected virtual void OnShootServer()
    {
        nextFire = Time.time + weaponSettings.fireInterval;
        syncedAmmo.Value--;
        // ① 弾を生成
        //GameObject obj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        NetworkObject obj = ManagerLocator.Instance.AllNetworkObjectPool.GetNetworkObject(bulletPrefab, FirePoint.position, FirePoint.rotation);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        var job = Propaty.Job;
        //go.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<BulletBaseController>();

        bullet.BulletInit(Collector, job, weaponSettings.bulletSetting);
        // ③ ネットワークでSpawn
        obj.SpawnWithOwnership(OwnerClientId);
    }
    private IEnumerator Reload()
    {
        isReloading = true;
        if (IsClient)
        {
            PlayReloadSound();
        }
        float invReloadTime = 1f / ReloadTime;
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        for (float t = 0; t < ReloadTime; t += 0.1f)
        {
            UpdateProgress(t * invReloadTime);
            yield return wait01;
        }
        UpdateProgress(0);

        isReloading = false;
        UpdateCount(weaponSettings.maxAmmo, weaponSettings.maxAmmo);

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
        if (isReloading) return;
        UpdateCount(newVal, MaxAmmo);
        if (oldVal < newVal) return;        
        //オーナー以外
        if (!IsOwner && IsClient)
        {
            PlayShotSound();
        }
        
        if (newVal <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    public virtual void UpdateCount(int remainVal, int maxVal){}

    public virtual void UpdateProgress(float progress){}

    public virtual void PlayShotSound(){}
    public virtual void PlayCantSound(){}
    public virtual void PlayReloadSound(){} 
}
