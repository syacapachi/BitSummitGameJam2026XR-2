using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WeaponSettings")]
public class WeaponSettingsSO : ScriptableObject
{
    [Header("Gun Setting")]
    public float fireInterval = 0.2f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    
    [Header("Laser")]
    public float laserDistance = 50f;

    [Header("Bullet Setting")]
    public float speed = 20f;
    [Header("Damage")]
    [SerializeField] float damage = 1;
    public float Damage => damage;
}
