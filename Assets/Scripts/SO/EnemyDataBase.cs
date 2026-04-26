using System.Collections.Generic;
using System.Linq;
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
    public IReadOnlyDictionary<EnemySO,int> EnemyDataToIdDict
    {
        get 
        {
            Create();
            return enemyDataToIdDict;
        }
    }
    public IReadOnlyList<EnemySO> IdToEnemyList
    {
        get
        {
            return enemyDataArray.ToList();
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
    public int Length => enemyDataArray.Length;
    public EnemySO GetEnemyDataFromId(int id)
    {
        if (id < 0 || id >= IdToEnemyList.Count)
        {
            Debug.LogError($"EnemyData with ID {id} is not found in EnemyDataBase.");
            return null;
        }
        return IdToEnemyList[id];
    }

    private bool isInitialized = false;
    private void Create()
    {
        if (isInitialized) return;
        enemyDataToIdDict.Clear();
        for (int i = 0; i < enemyDataArray.Length; i++)
        {
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