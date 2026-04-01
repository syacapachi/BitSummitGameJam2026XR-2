using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WeaponSettings")]
public class WeaponSettingsSO : ScriptableObject
{
    [Header("Shooting")]
    public float fireInterval = 0.2f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    public float speed = 20f;

    [Header("Laser")]
    public float laserDistance = 50f;

    [Header("Damage")]
    [SerializeField] float damage = 1;
    public float Damage => damage;
    [Header("Target")]
    public DamageAvailableTarget target;
}
[Flags]
public enum DamageAvailableTarget
{
    Nothing = 0,
    Human = 1,
    Ghost = 1 << 2,
    Both = Human | Ghost,
}