using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "randomSetting", menuName = "Game/RandomSpawn/Setting")]
public class RandomSpawnSettingBase : ScriptableObject
{
    [Header("横軸は0~1,縦が時間")]
    [SerializeField] AnimationCurve spawnDuration = AnimationCurve.Linear(0, 12, 1, 10);
    [SerializeField] EnemySpawnSetting[] enemySpawnSettings;
    private float[] weightArray;
    private int[] spawnedArray;
    /// <summary>
    /// 現在、何体出現したかをリセットする関数。
    /// </summary>
    public void ResetSpawnCount()
    {
        EnsureCache();
        for (int i = 0; i < spawnedArray.Length; i++)
        {
            spawnedArray[i] = 0;
        }
    }
    public float GetSpawnDuration(float progress01) 
    { 
        return spawnDuration.Evaluate(progress01); 
    }
    /// <summary>
    /// 重みの合計を返す。
    /// </summary>
    /// <param name="progress01"> フェイズの進行度 0~1</param>
    /// <returns> 重みの合計 </returns>
    public float GetAllWeight(float progress01)
    {
        float allWeight = 0f;
        EnsureCache();
        for (int i = 0; i < enemySpawnSettings.Length; i++)
        {
            weightArray[i] = enemySpawnSettings[i].SpawnWeight(progress01);
            allWeight += weightArray[i];
        }
        return allWeight;
    }
    public EnemySpawnSetting ChooseEnemyByWeight(float progress01,float value01)
    {
        value01 = Mathf.Clamp01(value01);
        float allWeight = 0f;
        EnsureCache();
        for (int i = 0; i < enemySpawnSettings.Length; i++)
        {
            if (spawnedArray[i] >= enemySpawnSettings[i].MaxSpawn) 
            {
                weightArray[i] = 0f;
                continue;
            }
            weightArray[i] = enemySpawnSettings[i].SpawnWeight(progress01);
            allWeight += weightArray[i];
        }
        // 全部0以下なら抽選不可
        if (allWeight <= 0f)
        {
            return null;
        }
        // 抽選
        float r = allWeight * value01;
        for(int i = 0;i < enemySpawnSettings.Length; i++)
        {
            if (spawnedArray[i] >= enemySpawnSettings[i].MaxSpawn) continue;
            r -= weightArray[i];
            if(r < 0)
            {
                spawnedArray[i]++;
                return enemySpawnSettings[i];
            }
        }
        return null;
    }
    //配列の初期化関数
    private void EnsureCache()
    {
        int length = enemySpawnSettings.Length;

        if (weightArray == null || weightArray.Length != length)
            weightArray = new float[length];

        if (spawnedArray == null || spawnedArray.Length != length)
            spawnedArray = new int[length];
    }
    public IReadOnlyList<EnemySpawnSetting> EnemySpawnSettings => enemySpawnSettings;
}
