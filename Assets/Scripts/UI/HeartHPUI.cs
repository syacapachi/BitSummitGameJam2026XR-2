using Syacapachi.Attribute;
using System.Collections.Generic;
using UnityEngine;

public class HeartHPUI : MonoBehaviour
{
    [SerializeField] bool useHeart;
    [SerializeField] RectTransform prefabParent;
    [SerializeField] GameObject heartPrefab;
    readonly Queue<GameObject> activeQueue = new();
    [OnInspectorButton(showOnlyInPlayMode:true)]
    public void SetHP(int newHP)
    {
        if (!useHeart) return;
        if (newHP < 0) newHP = 0;
        if (activeQueue.Count == newHP) return;
        if(activeQueue.Count > newHP)
        {
            while(activeQueue.Count > newHP)
            {
                ManagerLocator.Instance.LocalObjectPool.Release(activeQueue.Dequeue());
            }
        }
        else
        {
            while (activeQueue.Count < newHP)
            {
                var obj = ManagerLocator.Instance.LocalObjectPool.Get(heartPrefab);
                obj.transform.SetParent(prefabParent);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                activeQueue.Enqueue(obj);
            }
        }
    }
}
