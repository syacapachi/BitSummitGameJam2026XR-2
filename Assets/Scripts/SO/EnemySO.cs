using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    [SerializeField] int ID;
    [SerializeField] string Name = "Enemy";
    [SerializeField] Sprite icon;
    [SerializeField] int HP = 100;
    [SerializeField] int scoreValue = 100;
    [SerializeField] int MoveSpeed = 100;
    [SerializeField] GameObject prefab;
    [SerializeField] EnemyWeaponSettingsSO enemyWeapon;

    public int Id => ID;
    public string EnemyName => Name;
    public Sprite Icon => icon;
    public int Hp => HP;
    public int ScoreValue => scoreValue;
    public int MoveSpeedValue => MoveSpeed;
    public GameObject Prefab => prefab;
    public EnemyWeaponSettingsSO EnemyWeapon => enemyWeapon;
}