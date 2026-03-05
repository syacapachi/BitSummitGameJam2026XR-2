using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Phase")]
public class PhaseSO : ScriptableObject
{
    [Header("Phase Display Setting")]
    public string phaseDisplayName;

    [Header("Phase Settings")]
    public float phaseTime = 30f;
    public int clearBonus = 500;

    [Header("Spawn Settings")]
    public SpawnData[] spawnList;

    [Header("Spawn Point Index")]
    public int[] usableSpawnPointIndex;
}

[System.Serializable]
public class SpawnData
{
    public EnemySO enemyType;
    public int count;
}