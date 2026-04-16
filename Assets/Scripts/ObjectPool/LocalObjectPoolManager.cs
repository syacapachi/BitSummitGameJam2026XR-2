using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LocalObjectPoolManager : MonoBehaviour
{
    [Serializable]
    struct PoolConfigObject
    {
        public GameObject Prefab;
        public int PrewarmCount;
    }
    [SerializeField] List<PoolConfigObject> pooledPrefabsList = new();
    /// <summary>
    /// インスタンスIDをキー、オブジェクトプールを値とする辞書。インスタンスIDはプレハブの識別に使用される。
    /// </summary>
    readonly Dictionary<int, ObjectPool<GameObject>> objectPoolDic = new();

    private void Start()
    {
        foreach (var config in pooledPrefabsList)
        {
            ResisterPrefab(config.Prefab, config.PrewarmCount);
        }
    }
    private void ResisterPrefab(GameObject prefab,int PrewarmCount)
    {
        static void OnRelease(GameObject obj)
        {
            obj.SetActive(false);
            if(obj.TryGetComponent<Rigidbody>(out var rb))
            {
                bool preKinematic = rb.isKinematic;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = rb.isKinematic;
            }
        }
        var pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: OnRelease,
                actionOnDestroy: obj => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: PrewarmCount,
                maxSize: 100
            );
        objectPoolDic.Add(prefab.GetInstanceID(), pool);
    }
    public GameObject Get(GameObject prefab)
    {
        if(!objectPoolDic.ContainsKey(prefab.GetInstanceID()))
        {
            Debug.LogWarning($"Prefab {prefab.name} is not registered in the pool. Registering it now.");
             ResisterPrefab(prefab, 0);
        }
        if (objectPoolDic.TryGetValue(prefab.GetInstanceID(), out var pool))
        {
            return pool.Get();
        }
        Debug.LogError($"Prefab {prefab.name} not found in pool and failed to register.");
        return null;
    }
    public void Release(GameObject prefab)
    {
        if (!objectPoolDic.ContainsKey(prefab.GetInstanceID()))
        {
            Debug.LogWarning($"Prefab {prefab.name} is not registered in the pool. Registering it now.");
            ResisterPrefab(prefab, 0);
        }
        if (objectPoolDic.TryGetValue(prefab.GetInstanceID(), out var pool))
        {
            pool.Release(prefab);
        }
        else
        {
            Debug.LogWarning($"Prefab {prefab.name} is not registered in the pool. Destroying object instead.");
            Destroy(prefab);
        }
    }
    /// <summary>
    /// 内部でコルーチンを使用して、指定された遅延時間後にオブジェクトをプールに返す。これにより、オブジェクトの寿命を管理しやすくなり、特定のイベント（例：エフェクトの終了）に合わせてオブジェクトを自動的にリリースできる。
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="delay"></param>
    public void Release(GameObject prefab, float delay)
    {
        StartCoroutine(ReleaseAfterDelay(prefab, delay));
    }
    private IEnumerator ReleaseAfterDelay(GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        Release(prefab);
    }
}
