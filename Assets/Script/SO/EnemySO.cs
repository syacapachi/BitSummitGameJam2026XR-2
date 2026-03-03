using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    public int HP;
    public int Damage;
    public float Speed;
    public int scoreValue;
    public GameObject prefab;
}