using Syacapachi.Attribute;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Phase")]
public class PhaseSO : ScriptableObject
{
    [Header("Phase Display Setting")]
    [SerializeField] LocalizeSimpleText phaseName;

    public string PhaseDisplayNameEN => phaseName.GetText(false);
    public string PhaseDisplayNameJP => phaseName.GetText(true);

    [Header("Phase Settings")]
    [SerializeField] float phaseTime = 30f;
    [SerializeField] int clearBonus = 500;
    [SerializeField] int enemyCapacity = 10;
    [SerializeField] bool useRandomSpawn = false;
    [SerializeField, EnableIf(nameof(useRandomSpawn))]
    RandomSpawnSettingBase randomSpawnSettings;
    private SpawnSetting setting = null;

    public SpawnSetting Setting
    {
        get
        {
            setting ??= new SpawnSetting(spawnEvents, enemyCapacity, phaseTime, useRandomSpawn, randomSpawnSettings);
            return setting;
        }
    }
    public float PhaseTime => phaseTime;
    public int ClearBonus => clearBonus;

    [Header("Spawn Events")]
    [SerializeField] SpawnEvent[] spawnEvents;  // ←ここを変更

    public SpawnEvent[] SpawnEvents => spawnEvents;
#if UNITY_EDITOR
    private void OnValidate()
    {
        setting = null;
    }
#endif
}

[System.Serializable]
public struct SpawnEvent
{
    [SerializeField] EnemySO enemyType;   // 敵の種類
    [SerializeField] int spawnPointIndex; // 出現位置
    [SerializeField] float spawnTime;     // 何秒後に出るか（phase開始から）
    [SerializeField] bool forceSpawn;

    public readonly EnemySO EnemyType => enemyType;
    public readonly int SpawnPointIndex => spawnPointIndex;
    public readonly float SpawnTime => spawnTime;

    public readonly bool ForceSpawn => forceSpawn;

    public SpawnEvent(EnemySO so, int spawnPointIndex, float spawnTime, bool forceSpawn = false)
    {
        enemyType = so;
        this.spawnPointIndex = spawnPointIndex;
        this.spawnTime = spawnTime;
        this.forceSpawn = forceSpawn;
    }
    public readonly override string ToString()
    {
        return $"Name = {enemyType.EnemyName}, Index = {spawnPointIndex}, Time = {spawnTime}, IsForce = {ForceSpawn}";
    }
}
[Serializable]
public sealed class SpawnSetting
{
    public readonly SpawnEvent[] CustomSpawnEvents;
    public readonly int MaxSpawn;
    public readonly float PhaseTime;
    public readonly bool UseRandomSpawn;
    public readonly RandomSpawnSettingBase RandomSpawnSettings;
    public SpawnSetting(SpawnEvent[] events, int maxSpawn, float phaseTime,bool useRandomSpawn, RandomSpawnSettingBase randomSpawnSettings)
    {
        CustomSpawnEvents = events;
        MaxSpawn = maxSpawn;
        PhaseTime = phaseTime;
        UseRandomSpawn = useRandomSpawn;
        RandomSpawnSettings = randomSpawnSettings;
    }
}