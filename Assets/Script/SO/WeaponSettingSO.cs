using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WeaponSettings")]
public class WeaponSettingsSO : ScriptableObject
{
    [Header("Shooting")]
    public float fireRate = 0.2f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;

    [Header("Laser")]
    public float laserDistance = 50f;
}