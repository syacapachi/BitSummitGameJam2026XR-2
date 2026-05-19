using Syacapachi.Attribute;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
public class NGun : GunController
{
    [Header("LocalBullet")]
    [SerializeField] GameObject localBulletPrefab;
    [Header("Fps")]
    [SerializeField] Transform playerHead;
    [Header("gun")]
    [SerializeField] LineRenderer laserLine;
    [SerializeField] NGunAudioObserver audioObserver;
    [SerializeField] AmmoUI ammoUI;
    [SerializeField] PlayerStats playerStats;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
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
            fireEvent.Register(Activate);
            StartCoroutine(LaserUpdateCoroutine());

            laserLine.enabled = XRSettings.isDeviceActive;
        }
        else
        {
            //レーザー(補助用)はオーナーのみ
            laserLine.enabled = false;
        }
        if (IsServer)
        {
            gameStateEvent.Register(OnGameStateChanged);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            fireEvent.Unregister(Activate);
        }
        if (IsServer)
        {
            gameStateEvent.Unregister(OnGameStateChanged);
        }
    }
    private void OnGameStateChanged(GameState gameState)
    {
        switch(gameState)
        {
            case GameState.Home:
            case GameState.Initializing:
                syncedAmmo.Value = WeaponSettings.maxAmmo; break;
        }
    }
    public override void Activate()
    {
        base.Activate();
        //ローカルで弾を打つ打つことで、ラグさを見せない
        if (CurrentAmmo <= 0)
        {
            PlayCantSound();
            return;
        }
        var Locator = ManagerLocator.Instance;
        if (Locator == null || Locator.GameStateManager == null || Locator.LocalObjectPool == null) return;
        if (!Locator.GameStateManager.IsGamePlaying) return;
        var obj = Locator.LocalObjectPool.Get(localBulletPrefab);
        obj.transform.SetPositionAndRotation(FirePoint.position, FirePoint.rotation);
        if (obj.TryGetComponent<LocalBullet>(out var localBullet))
        {
            localBullet.BulletInit(WeaponSettings.bulletSetting);
        }
        //音をローカルですぐ流す
        PlayShotSound();
    }
    //オーナー以外で毎フレームチェックさせるオーバーヘッドをなくすためコルーチン化
    IEnumerator LaserUpdateCoroutine()
    {
        while (IsOwner)
        {
            UpdateLaser();
            yield return null;
        }
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
    public override void PlayCantSound()
    {
        audioObserver.PlayCantSound();
    }

    public override void PlayShotSound()
    {
        audioObserver.PlayShotSound();
    }
}