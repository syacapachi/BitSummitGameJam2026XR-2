using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemySO : ScriptableObject
{
    public int ID;
    public string Name = "Enemy";
    public Sprite icon;
    public int HP = 100;
    public int Damage = 100;
    public float BulletSpeed = 5;
    public float FirstShootDelay = 10;
    public float shootInterval = 10;
    public int scoreValue = 100;
    public GameObject prefab;
}