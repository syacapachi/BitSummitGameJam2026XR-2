using UnityEngine;

[CreateAssetMenu(fileName = "BulletSetting", menuName = "Weapons/BulletSetting")]
public class BulletSetting : ScriptableObject
{
    [Header("Speed")]
    [SerializeField] int speed = 100;
    [Header("Damage")]
    [SerializeField] float damage = 100;
    public float Damage => damage;
    public int Speed => speed;
}
