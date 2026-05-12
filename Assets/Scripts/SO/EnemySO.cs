using Syacapachi.Attribute;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    [SerializeField] string Name = "Enemy";
    [SerializeField] Sprite icon;
    /// <summary>
    /// 上手くもっていけないのでいったん放置します。
    /// OnNetworkSpawn以降じゃないとRpcや、NetworkVariableが有効ではない。
    /// しかし、Job同期前にApplySettingを行うと、見えてはいけないものがみえちゃう。
    /// </summary>
    [SerializeField, SingleFlagOnly] PlayerJob enemyJob;
    [SerializeField] int HP = 100;
    [SerializeField] int scoreValue = 100;
    [SerializeField] bool canMove = true;
    [SerializeField, EnableIf(nameof(canMove))] float MoveSpeed = 1;
    [SerializeField] GameObject prefab;
    [SerializeField] bool canAttack = true;
    [SerializeField, EnableIf(nameof(canAttack))] EnemyWeaponSettingsSO enemyWeapon;
    public string EnemyName => Name;
    public Sprite Icon => icon;
    public PlayerJob EnemyJob => enemyJob;
    public int Hp => HP;
    public int ScoreValue => scoreValue;
    public bool CanMove => canMove;
    public float MoveSpeedValue => MoveSpeed;
    public GameObject Prefab => prefab;
    public bool CanAttack => canAttack;
    public EnemyWeaponSettingsSO EnemyWeapon => enemyWeapon;
#if UNITY_EDITOR
    private void OnValidate()
    {
        if(prefab != null)
        {
            NEnemy nenemy = prefab.GetComponentInChildren<NEnemy>();
            if(nenemy != null)
            {
                nenemy.EnemyJob = enemyJob;
            }
        }
    }
#endif
}