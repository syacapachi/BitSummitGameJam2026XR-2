using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Phase")]
public class PhaseSO : ScriptableObject
{
    [Header("Phase Display Setting")]
    public string phaseDisplayName;

    [Header("Phase Settings")]
    public float phaseTime = 30f;
    public int clearBonus = 500;

    [Header("Spawn Events")]
    public SpawnEvent[] spawnEvents;  // ←ここを変更
}

[System.Serializable]
public class SpawnEvent
{
    public EnemySO enemyType;   // 敵の種類
    public int spawnPointIndex; // 出現位置
    public float spawnTime;     // 何秒後に出るか（phase開始から）
}