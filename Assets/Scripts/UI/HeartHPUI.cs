using Syacapachi.Attribute;
using System.Collections.Generic;
using UnityEngine;

public class HeartHPUI : MonoBehaviour
{
    [SerializeField] bool useHeart;
    [SerializeField] float breakTime = 2f;
    [SerializeField] RectTransform prefabParent;
    [SerializeField] GameObject heartPrefab;
    readonly Stack<HeartBreakController> activeStack = new();
    [OnInspectorButton(showOnlyInPlayMode:true)]
    public void SetHP(int newHP)
    {
        if (!useHeart) return;
        if (newHP < 0) newHP = 0;
        if (activeStack.Count == newHP) return;
        if(activeStack.Count > newHP)
        {
            while(activeStack.Count > newHP)
            {
                var breakHeart = activeStack.Pop();
                breakHeart.StartBreak(breakTime);
                ManagerLocator.Instance.LocalObjectPool.Release(breakHeart.gameObject, breakTime + 0.1f);
            }
        }
        else
        {
            while (activeStack.Count < newHP)
            {
                var obj = ManagerLocator.Instance.LocalObjectPool.Get(heartPrefab);
                obj.transform.SetParent(prefabParent);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var script = obj.GetComponent<HeartBreakController>();
                script.ResetHeart();
                activeStack.Push(script);
            }
        }
    }
}
