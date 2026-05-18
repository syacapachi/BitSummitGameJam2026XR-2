using System;
using UnityEngine;

[CreateAssetMenu(fileName = "enemySetting", menuName = "Game/RandomSpawn/EnemySpawnSetting")]
public class EnemySpawnSetting : ScriptableObject
{
    [SerializeField] EnemySO targetEnemy;
    
    [Header("時間は0~1,値は出現の重み")]
    [SerializeField] AnimationCurve spanwWeight = AnimationCurve.Linear(0,0,1,1);
    [SerializeField] int maxSpawn;
    [SerializeField] SpawnPointTags spawnPointTag = (SpawnPointTags)(-1);
    public EnemySO TargetEnemy => targetEnemy;
    public int MaxSpawn => maxSpawn;
    public SpawnPointTags SpawnPointTag => spawnPointTag;
    public float SpawnWeight(float phaseprogress) => spanwWeight.Evaluate(phaseprogress);
}
