using Syacapachi.util;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour, ICountDownUI, IProgressUI, IShotSound, IReloadSound
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

    private static readonly WaitForSeconds wait01 = new WaitForSeconds(0.1f);
    private float nextFire;
    //こいつは、残段数のみを同期してればわかる
    private bool isReloading = false;
    private SyncroPropaty Propaty;

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
            Propaty = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponentInChildren<SyncroPropaty>();
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
        NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(bulletPrefab,firePoint.position,firePoint.rotation);

        // ② 弾のLayerをプレイヤーのJobに合わせる
        GameObject go = obj.gameObject;
        var job = Propaty.Job;
        //go.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        var bullet = obj.GetComponent<BulletBaseController>();

        bullet.BulletInit(OwnerClientId,job,weaponSettings);
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
            PlayReloadSound();
        }
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        // ここでリロードのアニメーションやエフェクトを再生することができます。
        for (float t = 0; t < ReloadTime; t += 0.1f)
        {
            UpdateProgress(t / ReloadTime);
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
        if(isReloading) return;
        if(oldVal < newVal) return;
        UpdateCount(newVal, MaxAmmo);
        if (IsClient)
        {
            if (allReloadSoundSource != null)
            {
                allShootSoundSource.Play();
            }
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

    public virtual void PlayReloadSound(){} 
}
