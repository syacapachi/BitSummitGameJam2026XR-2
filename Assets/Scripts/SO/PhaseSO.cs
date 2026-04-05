using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Phase")]
public class PhaseSO : ScriptableObject
{
    [Header("Phase Display Setting")]
    [SerializeField] string phaseDisplayName;

    public string PhaseDisplayName => phaseDisplayName;

    [Header("Phase Settings")]
    [SerializeField] float phaseTime = 30f;
    [SerializeField] int clearBonus = 500;

    public float PhaseTime => phaseTime;
    public int ClearBonus => clearBonus;

    [Header("Spawn Events")]
    [SerializeField] SpawnEvent[] spawnEvents;  // ←ここを変更

    public SpawnEvent[] SpawnEvents => spawnEvents;
}

[System.Serializable]
public class SpawnEvent
{
    [SerializeField] EnemySO enemyType;   // 敵の種類
    [SerializeField] int spawnPointIndex; // 出現位置
    [SerializeField] float spawnTime;     // 何秒後に出るか（phase開始から）

    public EnemySO EnemyType => enemyType;
    public int SpawnPointIndex => spawnPointIndex;
    public float SpawnTime => spawnTime;
}