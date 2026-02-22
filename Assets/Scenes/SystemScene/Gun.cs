using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;

    public float fireRate = 0.2f;

    PlayerControls controls;
    float nextFire;

    void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Fire.performed += ctx => Shoot();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Shoot()
    {
        if (Time.time < nextFire) return;

        nextFire = Time.time + fireRate;

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }
}