using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NavMeshを使って、敵を移動させる。
/// </summary>
public class CheckPointManager : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] GameObject m_CheckPointParent;
#endif
    [SerializeField] Transform[] checkPoints;
    readonly Dictionary<Transform, int> transformToIndexDic = new();
    private void Awake()
    {
        transformToIndexDic.Clear();
        for(int i = 0; i < checkPoints.Length; i++)
        {
            transformToIndexDic[checkPoints[i]] = i;
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        
    }
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
