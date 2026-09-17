using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NavMeshを使って、敵を移動させる。
/// </summary>
[CreateAssetMenu(fileName = "CheckPointSetting", menuName = "ScriptableObjects/CheckPointSetting", order = 1)]
public class CheckPointSetting : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] GameObject m_CheckPointParent;
#endif
    [SerializeField] Transform[] checkPoints;
    readonly Dictionary<Transform, int> transformToIndexDic = new();
    private void OnEnable()
    {
        transformToIndexDic.Clear();
        for(int i = 0; i < checkPoints.Length; i++)
        {
            transformToIndexDic[checkPoints[i]] = i;
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if(m_CheckPointParent != null)
        {
            var childs = m_CheckPointParent.GetComponentsInChildren<Transform>();
            if (childs != null)
            {
                checkPoints = childs.Skip(1).ToArray();
            }
        }
    }
#endif
    public bool IsLastPoint(Transform transform)
    {
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            return val == checkPoints.Length - 1;
        }
        Debug.LogError("Transform is not assinged");
        return false;
    }
    public bool TryGetNextPoint(Transform transform, out Transform nextPoint)
    {
        nextPoint = null;
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            if (val < checkPoints.Length - 1)
            {
                nextPoint = checkPoints[val + 1];
                return true;
            }
            else
            {
                LogScope.Warning("This is the last point. No next point available.");
                return false;
            }
        }
        LogScope.Error("Transform is not assinged");
        return false;
    }
    public Transform GetNextPoint(Transform transform)
    {
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            return GetNextPoint(val);
        }
        Debug.LogError("Transform is not assinged");
        return null;
    }
    public Transform GetNextPoint(int index)
    {
        if(index < 0 || index >= checkPoints.Length)
        {
            Debug.LogError("List out Range");
            return null;
        }
        return (index == checkPoints.Length - 1) ? checkPoints[0] : checkPoints[index+1];
    }
}
