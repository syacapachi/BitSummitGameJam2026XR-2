using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/EnemyWeaponSettings")]
public class EnemyWeaponSettingsSO : WeaponSettingsSO
{
    [Header("Enemy Wait Times")]
    [SerializeField] float FirstShootDelay = 10;

    public float FirstShootDelayTime => FirstShootDelay;
}
