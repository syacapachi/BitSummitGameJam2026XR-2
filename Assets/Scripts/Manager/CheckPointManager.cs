using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CheckPointManager;

/// <summary>
/// NavMeshを使って、敵を移動させる。
/// </summary>
public class CheckPointManager : MonoBehaviour
{
    [Serializable]
    public readonly struct IndexToTransform
    {
        public readonly int id;
        public readonly Transform transform;
        public IndexToTransform(int id, Transform transform)
        {
            this.id = id;
            this.transform = transform;
        }
    }
#if UNITY_EDITOR
    [SerializeField] GameObject m_CheckPointParent;
#endif
    [SerializeField] Transform[] checkPoints;
    IndexToTransform[] indexToTransformArr;
    readonly Dictionary<IndexToTransform, int> transformToIndexDic = new();
    private void Awake()
    {
        transformToIndexDic.Clear();
        indexToTransformArr = new IndexToTransform[checkPoints.Length];
        for(int i = 0; i < checkPoints.Length; i++)
        {
            IndexToTransform indexToTransform = new(i, checkPoints[i]);
            indexToTransformArr[i] = indexToTransform;
            transformToIndexDic[indexToTransform] = i;
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
    public bool IsLastPoint(IndexToTransform transform)
    {
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            return val == checkPoints.Length - 1;
        }
        Debug.LogError("Transform is not assinged");
        return false;
    }
    public bool TryGetNextPoint(IndexToTransform transform, out IndexToTransform nextPoint)
    {
        nextPoint = default;
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            if (val < checkPoints.Length - 1)
            {
                nextPoint = indexToTransformArr[val + 1];
                return true;
            }
            else
            {
                Debug.LogWarning("This is the last point. No next point available.");
                return false;
            }
        }
        Debug.LogError("Transform is not assinged");
        return false;
    }
    public IndexToTransform GetNextPoint(IndexToTransform transform)
    {
        if (transformToIndexDic.TryGetValue(transform, out int val))
        {
            return GetNextPoint(val);
        }
        Debug.LogError("Transform is not assinged");
        return default;
    }
    public IndexToTransform GetNextPoint(int index)
    {
        if(index < 0 || index >= checkPoints.Length)
        {
            Debug.LogError("List out Range");
            return default;
        }
        return (index == checkPoints.Length - 1) ? indexToTransformArr[0] : indexToTransformArr[index+1];
    }
}
