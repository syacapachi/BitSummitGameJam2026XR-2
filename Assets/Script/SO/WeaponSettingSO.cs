using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WeaponSettings")]
public class WeaponSettingsSO : ScriptableObject
{
    [Header("Shooting")]
    public float fireRate = 0.2f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    public float speed = 20f;

    [Header("Laser")]
    public float laserDistance = 50f;

    [Header("Damage")]
    [SerializeField] float damage = 1;
    public float Damage => damage;
}