using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Phase")]
public class PhaseSO : ScriptableObject
{
    [Header("Phase Display Setting")]
    [SerializeField] string phaseDisplayNameEN;
    [SerializeField] string phaseDisplayNameJP;

    public string PhaseDisplayNameEN => phaseDisplayNameEN;
    public string PhaseDisplayNameJP => phaseDisplayNameJP;

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
public struct SpawnEvent
{
    [SerializeField] EnemySO enemyType;   // 敵の種類
    [SerializeField] int spawnPointIndex; // 出現位置
    [SerializeField] float spawnTime;     // 何秒後に出るか（phase開始から）

    public readonly EnemySO EnemyType => enemyType;
    public readonly int SpawnPointIndex => spawnPointIndex;
    public readonly float SpawnTime => spawnTime;
}