using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Syacapachi.util
{
    public class NetworkObjectPool : NetworkBehaviour
    {
        public static NetworkObjectPool Singleton { get; private set; }

        [System.Serializable]
        public struct PoolConfig
        {
            public GameObject Prefab;
            public int PrewarmCount;
        }

        [SerializeField]
        private List<PoolConfig> poolConfigs = new();

        class Pool
        {
            public GameObject prefab;
            public Queue<NetworkObject> objects = new();
        }

        private Dictionary<int, Pool> pools = new();

        void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
                return;
            }

            Singleton = this;
        }

        public override void OnNetworkSpawn()
        {
            Initialize();
        }

        void Initialize()
        {
            foreach (var config in poolConfigs)
            {
                RegisterPrefab(config.Prefab, config.PrewarmCount);
            }
        }

        public void RegisterPrefab(GameObject prefab, int prewarm)
        {
            int id = prefab.GetInstanceID();

            if (pools.ContainsKey(id))
                return;

            var pool = new Pool();
            pool.prefab = prefab;

            pools[id] = pool;

            for (int i = 0; i < prewarm; i++)
            {
                var obj = Create(prefab);
                Return(obj, prefab);
            }

            NetworkManager.Singleton.PrefabHandler.AddHandler(
                prefab,
                new PooledPrefabInstanceHandler(prefab, this)
            );
        }

        NetworkObject Create(GameObject prefab)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            return go.GetComponent<NetworkObject>();
        }

        Pool GetPool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();

            if (!pools.TryGetValue(id, out var pool))
            {
                RegisterPrefab(prefab, 0);
                pool = pools[id];
            }

            return pool;
        }

        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var pool = GetPool(prefab);

            NetworkObject obj;

            if (pool.objects.Count > 0)
            {
                obj = pool.objects.Dequeue();
            }
            else
            {
                obj = Create(prefab);
            }

            var t = obj.transform;

            t.SetPositionAndRotation(pos, rot);
            t.SetParent(null);
            t.localScale = Vector3.one;

            obj.gameObject.SetActive(true);

            return obj;
        }

        public NetworkObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var obj = GetNetworkObject(prefab, pos, rot);
            obj.Spawn();
            return obj;
        }

        public void Despawn(NetworkObject obj)
        {
            obj.Despawn(false);
        }

        public void Return(NetworkObject obj, GameObject prefab)
        {
            var pool = GetPool(prefab);

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);

            pool.objects.Enqueue(obj);
        }

        class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
        {
            GameObject prefab;
            NetworkObjectPool pool;

            public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
            {
                this.prefab = prefab;
                this.pool = pool;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return pool.GetNetworkObject(prefab, position, rotation);
            }

            public void Destroy(NetworkObject networkObject)
            {
                pool.Return(networkObject, prefab);
            }
        }
    }
}