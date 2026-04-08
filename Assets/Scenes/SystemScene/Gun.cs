using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;

public class Gun : MonoBehaviour
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

        // 開始点
        laserLine.SetPosition(0, firePoint.position);

        // Raycast で着弾点を判定
        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            // 当たった場合
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            // 当たらなかった場合
            laserLine.SetPosition(1, firePoint.position + forward * laserDistance);
        }
    }
    void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Fire.performed += ctx => Shoot();
        controls.Player.Marker.performed += ctx => PlaceMarker();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Shoot()
    {
        if (Time.time < nextFire) return;

        nextFire = Time.time + fireRate;

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    void PlaceMarker()
    {
        if (markerPrefab == null || firePoint == null) return;

        RaycastHit hit;
        Vector3 forward = firePoint.forward;

        if (Physics.Raycast(firePoint.position, forward, out hit, laserDistance))
        {
            // マーカーPrefabを生成
            GameObject markerObj = Instantiate(markerPrefab, hit.point, Quaternion.identity);

            // Marker.cs がついていれば色や寿命などの設定は自動で行われる
            Marker markerScript = markerObj.GetComponent<Marker>();
            if (markerScript != null)
            {
                markerScript.color = Color.red;   // 好きな色に変更可能
                markerScript.lifeTime = 5f;      // 消えるまでの時間
            }
        }
    }
}