using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    public int ID;
    public string Name = "Enemy";
    public Sprite icon;
    public int HP = 100;
    public int scoreValue = 100;
    public int MoveSpeed = 100;
    public GameObject prefab;
    public EnemyWeaponSettingsSO enemyWeapon;
}