using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
        public override bool Equals(object obj)
        {
            return obj is IndexToTransform other && Equals(other);
        }
        public override int GetHashCode()
        {
            return id;
        }
        public static bool operator ==(
            IndexToTransform left,
            IndexToTransform right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            IndexToTransform left,
            IndexToTransform right)
        {
            return !left.Equals(right);
        }
    }
    [Serializable]
    public class TagToTransfrom
    {
        [SerializeField] SpawnPointTags spawnPointTag;
        [SerializeField] SpawnPointMarker[] spawnPointmarkers;

        public SpawnPointTags SpawnPointTags => spawnPointTag;
        public SpawnPointMarker[] SpawnPointMarkers => spawnPointmarkers;
    }
#if UNITY_EDITOR
    [SerializeField] GameObject m_CheckPointParent;
    private GameObject lastCheckPoint;
#endif
    [SerializeField] Transform[] checkPoints;
    [SerializeField] TagToTransfrom[] tagToTransfroms;
    IndexToTransform[] indexToTransformArr;
    bool[] usePointArr;
    List<IndexToTransform> cacheList = new();
    public IndexToTransform[] SpawnPoints => indexToTransformArr;
    FlagTable<IndexToTransform> flagTable;
    private void Awake()
    {
        //全てのスポーンポイントの配列化
        indexToTransformArr = new IndexToTransform[checkPoints.Length];
        for (int i = 0; i < checkPoints.Length; i++)
        {
            IndexToTransform indexToTransform = new(i, checkPoints[i]);
            indexToTransformArr[i] = indexToTransform;
        }
        usePointArr = new bool[checkPoints.Length];

        //最大bit取得
        int maxBit = 0;
        foreach (var tags in tagToTransfroms)
        {
            int value = (int)tags.SpawnPointTags;

            if (value > 0)
            {
                int bit = GetBitIndex(value);

                if (bit > maxBit)
                    maxBit = bit;
            }
        }

        //タグ(Flag)ごとの場所
        IndexToTransform[][] indexToTransforms = new IndexToTransform[maxBit + 1][];
        foreach (var tags in tagToTransfroms)
        {
            int value = (int)tags.SpawnPointTags;
            //0はフラグにないのでスキップ
            if (value == 0)
            {
                continue;
            }
            if (value != -1 &&(value & (value - 1)) != 0)
            {
                Debug.LogError("Multiple flags are not allowed");
                continue;
            }
            int bit = GetBitIndex(value);

            if (indexToTransforms[bit] != null)
            {
                Debug.LogError($"{tags.SpawnPointTags} is Already Assginned", gameObject);
                continue;
            }
            IndexToTransform[] inlineArray = new IndexToTransform[tags.SpawnPointMarkers.Length];
            for(int i = 0;i < tags.SpawnPointMarkers.Length; i++)
            {
                inlineArray[i] = indexToTransformArr[tags.SpawnPointMarkers[i].SpawnPointId];
            }
            indexToTransforms[bit] = inlineArray;
        }
        flagTable = new FlagTable<IndexToTransform>(indexToTransforms);
    }
    static int GetBitIndex(int value)
    {
        if (value == 0) 
            throw new ArgumentException(
            "Value must contain exactly one bit.");
        int bit = 0;

        while ((value & 1) == 0)
        {
            value >>= 1;
            bit++;
        }
        return bit;
    }
    public void GetSpawnPointByTag(SpawnPointTags tags, List<IndexToTransform> result)
    {
        flagTable.Collect((int)tags, result);
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
    public int GetEnablePointByTag(SpawnPointTags tags)
    {
        GetSpawnPointByTag(tags, cacheList);
        for (int i = 0; i < cacheList.Count; i++)
        {
            if (!usePointArr[cacheList[i].id]) return i;
        }
        return -1;
    }
    public IndexToTransform GetRandomPoint()
    {
        return indexToTransformArr[UnityEngine.Random.Range(0, checkPoints.Length)];
    }
#if UNITY_EDITOR
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
[Flags]
public enum SpawnPointTags
{
    Nothing = 0,
    Front = 1 << 0,
    Back = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3
}