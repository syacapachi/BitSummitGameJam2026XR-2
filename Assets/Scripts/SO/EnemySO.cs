using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    [SerializeField] string Name = "Enemy";
    [SerializeField] Sprite icon;
    [SerializeField] int HP = 100;
    [SerializeField] int scoreValue = 100;
    [SerializeField] float MoveSpeed = 1;
    [SerializeField] GameObject prefab;
    [SerializeField] EnemyWeaponSettingsSO enemyWeapon;
    public string EnemyName => Name;
    public Sprite Icon => icon;
    public int Hp => HP;
    public int ScoreValue => scoreValue;
    public float MoveSpeedValue => MoveSpeed;
    public GameObject Prefab => prefab;
    public EnemyWeaponSettingsSO EnemyWeapon => enemyWeapon;
}