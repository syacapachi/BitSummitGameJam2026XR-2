using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    [SerializeField] GameObject m_Object;
    [SerializeField] List<Transform> checkPointList = new();
    readonly Dictionary<Transform, int> transformToIndexDic = new();
#if UNITY_EDITOR
    private void Reset()
    {
        
    }
    private void OnValidate()
    {
        if(m_Object != null)
        {
            foreach(Transform child in m_Object.GetComponentsInChildren<Transform>())
            {
                checkPointList.Add(child);
            }
            m_Object = null;
            transformToIndexDic.Clear();
            for(int i=0;i<checkPointList.Count;i++)
            {
                transformToIndexDic.Add(checkPointList[i], i);
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
        if(index < 0 || index >= checkPointList.Count)
        {
            Debug.LogError("List out Range");
            return null;
        }
        return (index == checkPointList.Count-1) ? checkPointList[0] : checkPointList[index+1];
    }
}
