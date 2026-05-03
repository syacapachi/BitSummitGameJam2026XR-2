using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyDataBase", menuName = "Game/DifficultyDataBase")]
public class DifficultyDataBase : ScriptableObject
{
    [SerializeField] DifficultySetting[] settings;
    readonly Dictionary<Difficulty, DifficultySetting> difficultyDataDic = new();
    private bool isInitialize;
    public IReadOnlyDictionary<Difficulty, DifficultySetting> DifficultyDataDic
    {
        get
        {
            CreateDic();
            return difficultyDataDic;
        }
    }
    public DifficultySetting GetSetting(Difficulty setting)
    {
        CreateDic();
        return difficultyDataDic[setting];
    }
    public bool TryGetSetting(Difficulty setting, out DifficultySetting settingValue)
    {
        CreateDic();
        if(difficultyDataDic.TryGetValue(setting, out settingValue))
        {
            return true;
        }
        return false;
    }
    private void CreateDic()
    {
        if (isInitialize) return;
        isInitialize = true;
        foreach (var setting in settings)
        {
            difficultyDataDic[setting.Difficulty] = setting;
        }
#if UNITY_EDITOR
        foreach (var diff in Enum.GetValues(typeof(Difficulty)))
        {
            if (!difficultyDataDic.ContainsKey((Difficulty)diff))
            {
                Debug.LogError($"{(Difficulty)diff} is not setting");
            }
        }
#endif
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        isInitialize = false;
    }
#endif
}
