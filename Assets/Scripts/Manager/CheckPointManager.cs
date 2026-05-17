using Meta.WitAi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static CheckPointManager;

/// <summary>
/// NavMeshを使って、敵を移動させる。
/// </summary>
public class CheckPointManager : MonoBehaviour
{
    [Serializable]
    public readonly struct IndexToTransform : IEquatable<IndexToTransform>
    {
        public readonly int id;
        public readonly Transform transform;
        public IndexToTransform(int id, Transform transform)
        {
            this.id = id;
            this.transform = transform;
        }

        public bool Equals(IndexToTransform other)
        {
            return id == other.id;
        }
    }
#if UNITY_EDITOR
    [SerializeField] GameObject m_CheckPointParent;
    private GameObject lastCheckPoint;
#endif
    [SerializeField] Transform[] checkPoints;
    IndexToTransform[] indexToTransformArr;
    bool[] usePointArr;
    readonly Dictionary<IndexToTransform, int> transformToIndexDic = new();

    public IndexToTransform[] SpawnPoints => indexToTransformArr;
    private void Awake()
    {
        transformToIndexDic.Clear();
        indexToTransformArr = new IndexToTransform[checkPoints.Length];
        for (int i = 0; i < checkPoints.Length; i++)
        {
            IndexToTransform indexToTransform = new(i, checkPoints[i]);
            indexToTransformArr[i] = indexToTransform;
            transformToIndexDic[indexToTransform] = i;
        }
        usePointArr = new bool[checkPoints.Length];
    }
    public bool IsUsingPoint(int index)
    {
        if (index < 0 || usePointArr.Length <= index) return false;
        return usePointArr[index];
    }
    public bool TrySetUsePoint(int index, bool use)
    {
        //範囲外失敗
        if (index < 0 || usePointArr.Length <= index) return false;
        //値同じ失敗
        if (usePointArr[index] == use) return false;
        usePointArr[index] = use;
        return true;
    }
    public int GetEnablePoint()
    {
        for(int i = 0;i < usePointArr.Length;i++)
        {
            if(!usePointArr[i]) return i;
        }
        return -1;
    }
    //public bool IsLastPoint(IndexToTransform transform)
    //{
    //    if (transformToIndexDic.TryGetValue(transform, out int val))
    //    {
    //        return val == checkPoints.Length - 1;
    //    }
    //    Debug.LogError("Transform is not assinged");
    //    return false;
    //}
    //public bool TryGetNextPoint(IndexToTransform transform, out IndexToTransform nextPoint)
    //{
    //    nextPoint = default;
    //    if (transformToIndexDic.TryGetValue(transform, out int val))
    //    {
    //        if (val < checkPoints.Length - 1)
    //        {
    //            nextPoint = indexToTransformArr[val + 1];
    //            return true;
    //        }
    //        else
    //        {
    //            Debug.LogWarning("This is the last point. No next point available.");
    //            return false;
    //        }
    //    }
    //    Debug.LogError("Transform is not assinged");
    //    return false;
    //}
    //public IndexToTransform GetNextPoint(IndexToTransform transform)
    //{
    //    if (transformToIndexDic.TryGetValue(transform, out int val))
    //    {
    //        return GetNextPoint(val);
    //    }
    //    Debug.LogError("Transform is not assinged");
    //    return default;
    //}
    //public IndexToTransform GetNextPoint(int index)
    //{
    //    if(index < 0 || index >= checkPoints.Length)
    //    {
    //        Debug.LogError("List out Range");
    //        return GetRandomPoint();
    //    }
    //    return (index == checkPoints.Length - 1) ? indexToTransformArr[0] : indexToTransformArr[index+1];
    //}
    public IndexToTransform GetRandomPoint()
    {
        return indexToTransformArr[UnityEngine.Random.Range(0, checkPoints.Length)];
    }
#if UNITY_EDITOR
    private void Reset()
    {

    }
    private void OnValidate()
    {
        if (m_CheckPointParent != null)
        {
            if (lastCheckPoint != null && lastCheckPoint != m_CheckPointParent)
            {
                var lastSpawnPoints = lastCheckPoint.GetComponentsInChildren<Transform>();
                foreach (var spawnPoint in lastSpawnPoints.Skip(1))
                {
                    if (spawnPoint.TryGetComponent<SpawnPointMarker>(out var marker))
                    {
                        //次のフレームで1っ回だけ実行される
                        EditorApplication.delayCall += () =>
                        {
                            //Destroyは危険な処理なのでOnValidateでは実行できない
                            if (marker != null) DestroyImmediate(marker);
                        };

                    }
                    if (spawnPoint.TryGetComponent<Collider>(out var collider))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (collider != null) DestroyImmediate(collider);
                        };
                    }
                }
            }
            lastCheckPoint = m_CheckPointParent;
            var childs = m_CheckPointParent.GetComponentsInChildren<Transform>();
            int id = 0;
            //親を飛ばす
            checkPoints = childs.Skip(1).ToArray();
            foreach (var t in checkPoints)
            {
                if (!t.TryGetComponent<SpawnPointMarker>(out var spawnPoint))
                {
                    //AddComponentは重い処理なので、エラーを防ぐため次のフレームで実行する。
                    //コピーしないと全部同じになる。(内部参照が同じになるため)
                    int next = id;
                    EditorApplication.delayCall += () => CreateMaker(t, next);
                }
                else
                {
                    spawnPoint.SpawnPointId = id;
                }
                id++;
            }
        }
    }
    void CreateMaker(Transform t, int id)
    {
        SphereCollider sc = t.gameObject.AddComponent<SphereCollider>();
        sc.radius = 0.1f;
        sc.isTrigger = true;
        SpawnPointMarker spawnPoint = t.gameObject.AddComponent<SpawnPointMarker>();
        spawnPoint.SpawnPointId = id;
    }
#endif
}
