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
    [SerializeField] PhaseTimeMode phaseMode = PhaseTimeMode.TimeLimit;
    [Tooltip("TimeLimitなら、終わる時間、")]
    [SerializeField] float phaseTime = 30f;
    [SerializeField] int clearBonus = 500;
    [SerializeField] int enemyCapacity = 10;
    [SerializeField] WaitSpawnType waitSpawnType = WaitSpawnType.WaitForNext;
    [SerializeField, EnableIfEnum(nameof(waitSpawnType), true, WaitSpawnType.WaitForSomeEnemyDead)]
    int waitSpawnRemainCount = 5;
    [SerializeField] NextPhaseType nextPhaseType = NextPhaseType.Remain;
    [SerializeField] bool useRandomSpawn = false;
    [SerializeField, EnableIf(nameof(useRandomSpawn))]
    RandomSpawnSettingBase randomSpawnSettings;
    /// <summary>
    /// 設定のキャッシュ。エディター上で変更しても適応されません。
    /// </summary>
    private SpawnSetting setting = null;
    /// <summary>
    /// NetworkEnemySpawnerに送る出現設定。
    /// </summary>
    public SpawnSetting Setting
    {
        get
        {
            setting ??= new SpawnSetting(spawnEvents, enemyCapacity, waitSpawnType, waitSpawnRemainCount, nextPhaseType, phaseTime, useRandomSpawn, randomSpawnSettings);
            return setting;
        }
    }
    /// <summary>
    /// フェイズの終了モード
    /// </summary>
    public PhaseTimeMode PhaseMode => phaseMode;
    /// <summary>
    /// 1フェイズの時間
    /// </summary>
    public float PhaseTime => phaseTime;
    /// <summary>
    /// フェイズのクリアボーナス点
    /// </summary>
    public int ClearBonus => clearBonus;

    [Header("Spawn Events")]
    [SerializeField] SpawnEvent[] spawnEvents;  // ←ここを変更
#if UNITY_EDITOR
    private void OnValidate()
    {
        setting = null;
    }
#endif
}
public enum PhaseTimeMode
{
    /// <summary>
    /// 終わる時間があります。
    /// </summary>
    TimeLimit,
    /// <summary>
    /// すべての敵が死ぬまで終わらないモードです。
    /// </summary>
    AllEnemyKill,
    /// <summary>
    /// HPがなくなるまで終わらないモードです。
    /// </summary>
    Endress
}

[System.Serializable]
public struct SpawnEvent
{
    /// <summary>
    /// 敵の種類
    /// </summary>
    [SerializeField] EnemySO enemyType;
    /// <summary>
    /// 出現位置
    /// </summary>
    [SerializeField] int spawnPointIndex;
    /// <summary>
    /// 何秒後に出るか（phase開始から）
    /// </summary>
    [SerializeField] float spawnTime;    
    /// <summary>
    /// 出現上限でも出すか
    /// </summary>
    [SerializeField] bool forceSpawn;
    /// <summary>
    /// 敵の種類
    /// </summary>
    public readonly EnemySO EnemyType => enemyType;
    /// <summary>
    /// 出現位置
    /// </summary>
    public readonly int SpawnPointIndex => spawnPointIndex;
    /// <summary>
    /// 何秒後に出るか（phase開始から）
    /// </summary>
    public readonly float SpawnTime => spawnTime;
    /// <summary>
    /// 出現上限でも出すか
    /// </summary>
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
        return $"Name = {enemyType.EnemyName}, Index = {spawnPointIndex}, TimeLimit = {spawnTime}, IsForce = {ForceSpawn}";
    }
}
[Serializable]
public sealed class SpawnSetting
{
    /// <summary>
    /// インスペクター上で設定された出現設定
    /// </summary>
    public readonly SpawnEvent[] CustomSpawnEvents;
    /// <summary>
    /// 出現上限
    /// </summary>
    public readonly int MaxSpawn;
    /// <summary>
    /// 上限に達した場合、再出現の条件
    /// </summary>
    public readonly WaitSpawnType WaitSpawnType;
    /// <summary>
    /// <param name="WaitSpawnType"> 
    /// WaitSpawnType
    /// </param>がWaitSpawnType.WaitForSomeEnemyDeadの場合の数学
    /// </summary>
    public readonly int WaitSpawnRemainCount;
    /// <summary>
    /// 次のフェイズに行った時、待っている敵をどうするか
    /// </summary>
    public readonly NextPhaseType NextPhaseType;
    /// <summary>
    /// 1フェイズの時間
    /// </summary>
    public readonly float PhaseTime;
    /// <summary>
    /// ランダムスポーンを使うかどうか
    /// </summary>
    public readonly bool UseRandomSpawn;
    /// <summary>
    /// ランダムスポーンの設定
    /// </summary>
    public readonly RandomSpawnSettingBase RandomSpawnSettings;
    public SpawnSetting(SpawnEvent[] events, int maxSpawn, WaitSpawnType waitSpawnType,int waitSpawnRemainCount, NextPhaseType nextPhaseType, float phaseTime, bool useRandomSpawn, RandomSpawnSettingBase randomSpawnSettings)
    {
        CustomSpawnEvents = events;
        WaitSpawnType = waitSpawnType;
        WaitSpawnRemainCount = waitSpawnRemainCount;
        NextPhaseType = nextPhaseType;
        MaxSpawn = maxSpawn;
        PhaseTime = phaseTime;
        UseRandomSpawn = useRandomSpawn;
        RandomSpawnSettings = randomSpawnSettings;
    }
}