using UnityEngine;

[CreateAssetMenu(fileName = "enemySetting", menuName = "Game/RandomSpawn/EnemySpawnSetting")]
public class EnemySpawnSetting : ScriptableObject
{
    [SerializeField] EnemySO targetEnemy;
    
    [Header("時間は0~1,値は出現の重み")]
    [SerializeField] AnimationCurve spanwWeight = AnimationCurve.Linear(0,0,1,1);
    [SerializeField] int maxSpawn;
    [SerializeField] int[] spawnIndexes;
    public EnemySO TargetEnemy => targetEnemy;
    public int MaxSpawn => maxSpawn;
    public int[] SpawnIndexes => spawnIndexes;
    public float SpawnWeight(float phaseprogress) => spanwWeight.Evaluate(phaseprogress);
#if UNITY_EDITOR
    private void OnValidate()
    {
        for(int i=0;i<spanwWeight.keys.Length;i++)
        {
            var key = spanwWeight.keys[i];
            if (key.time < 0) { key.time = 0f; }
            if (key.time > 1f) { key.time = 1f; }
        }
    }
#endif
}
