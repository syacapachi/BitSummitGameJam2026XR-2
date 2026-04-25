using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemySOをIDで管理するScriptableObject
/// 通信で、EnemySOをIDで送るために必要
/// </summary>
[CreateAssetMenu(fileName = "EnemyDataBase", menuName = "ScriptableObjects/EnemyDataBase", order = 1)]
public class EnemyDataBase : ScriptableObject
{
    [Header("EnemySOと配列のインデックスを対応させる")]
    [Tooltip("EnemySOと配列のインデックスを対応させます。配列のインデックスをIDとして取得できます。")]
    [SerializeField] EnemySO[] enemyDataArray;
    private readonly Dictionary<EnemySO,int> enemyDataToIdDict = new();
    private readonly Dictionary<int, EnemySO> idToEnemyDataDict = new();
    public IReadOnlyDictionary<EnemySO,int> EnemyDataToIdDict
    {
        get 
        {
            Create();
            return enemyDataToIdDict;
        }
    }
    public IReadOnlyDictionary<int ,EnemySO> IdToEnemyDataDict
    {
        get
        {
            Create();
            return idToEnemyDataDict;
        }
    }
    public int GetIdFromEnemyData(EnemySO data)
    {
        Create();
        if (!EnemyDataToIdDict.ContainsKey(data))
        {
            Debug.LogError($"EnemyData {data.name} is not found in EnemyDataBase.");
            return -1;
        }
        return EnemyDataToIdDict[data];
    }
    public EnemySO GetEnemyDataFromId(int id)
    {
        Create();
        if (!IdToEnemyDataDict.ContainsKey(id))
        {
            Debug.LogError($"EnemyData with ID {id} is not found in EnemyDataBase.");
            return null;
        }
        return IdToEnemyDataDict[id];
    }

    private bool isInitialized = false;
    private void Create()
    {
        if (isInitialized) return;
        enemyDataToIdDict.Clear();
        idToEnemyDataDict.Clear();
        for (int i = 0; i < enemyDataArray.Length; i++)
        {
            idToEnemyDataDict[i] = enemyDataArray[i];
            enemyDataToIdDict[enemyDataArray[i]] = i;
        }
        isInitialized = true;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        isInitialized = false;
    }
#endif
}