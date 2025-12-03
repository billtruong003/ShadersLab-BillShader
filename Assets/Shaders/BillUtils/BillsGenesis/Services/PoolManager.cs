using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    public sealed class PoolManager : GenesisSingletonService<PoolManager>
    {
        [Serializable]
        public class PoolConfig
        {
            public GameObject Prefab;
            public int InitialSize = 10;
        }

        [SerializeField] private List<PoolConfig> _prewarmConfigs = new List<PoolConfig>();

        private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
        private readonly Dictionary<int, int> _instanceIdToPrefabId = new Dictionary<int, int>();
        private Transform _root;

        public override Task InitializeAsync()
        {
            _root = new GameObject("Pool_System_Root").transform;
            DontDestroyOnLoad(_root);
            _root.SetParent(transform);

            for (int i = 0; i < _prewarmConfigs.Count; i++)
            {
                if (_prewarmConfigs[i].Prefab)
                {
                    CreatePool(_prewarmConfigs[i]);
                }
            }
            return Task.CompletedTask;
        }

        public T Spawn<T>(T prefab) where T : Component => Spawn(prefab.gameObject, Vector3.zero, Quaternion.identity, null).GetComponent<T>();
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component => Spawn(prefab.gameObject, position, rotation, null).GetComponent<T>();
        public T Spawn<T>(T prefab, Transform parent) where T : Component => Spawn(prefab.gameObject, Vector3.zero, Quaternion.identity, parent).GetComponent<T>();
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component => Spawn(prefab.gameObject, position, rotation, parent).GetComponent<T>();
        public GameObject Spawn(GameObject prefab) => Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        public GameObject Spawn(GameObject prefab, Transform parent) => Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) => Spawn(prefab, position, rotation, null);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!prefab) return null;

            int prefabId = prefab.GetInstanceID();
            if (!_pools.TryGetValue(prefabId, out var pool))
            {
                pool = CreatePool(new PoolConfig { Prefab = prefab, InitialSize = 1 });
            }

            GameObject instance = pool.Get();
            int instanceId = instance.GetInstanceID();

            _instanceIdToPrefabId[instanceId] = prefabId;

            var t = instance.transform;
            t.SetPositionAndRotation(position, rotation);
            if (parent) t.SetParent(parent);

            instance.SetActive(true);
            pool.TriggerOnSpawn(instanceId);

            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (!instance) return;

            int instanceId = instance.GetInstanceID();
            if (_instanceIdToPrefabId.TryGetValue(instanceId, out int prefabId) && _pools.TryGetValue(prefabId, out var pool))
            {
                pool.TriggerOnDespawn(instanceId);
                instance.SetActive(false);
                pool.Release(instance);
                _instanceIdToPrefabId.Remove(instanceId);
            }
            else
            {
                Destroy(instance);
            }
        }

        public void Despawn(Component component)
        {
            if (component) Despawn(component.gameObject);
        }

        public void DespawnAll()
        {
            var activeIds = _instanceIdToPrefabId.Keys.ToArray();
            foreach (var id in activeIds)
            {
                if (!_instanceIdToPrefabId.TryGetValue(id, out int prefabId)) continue;
                if (!_pools.TryGetValue(prefabId, out var pool)) continue;

                var instance = pool.FindActiveInstance(id);
                if (instance)
                {
                    pool.TriggerOnDespawn(id);
                    instance.SetActive(false);
                    pool.Release(instance);
                }
            }
            _instanceIdToPrefabId.Clear();
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (!prefab) return;
            int prefabId = prefab.GetInstanceID();
            if (!_pools.TryGetValue(prefabId, out var pool))
            {
                CreatePool(new PoolConfig { Prefab = prefab, InitialSize = count });
            }
            else
            {
                pool.Expand(count);
            }
        }

        public void ClearPool(GameObject prefab)
        {
            if (!prefab) return;
            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Clear();
                _pools.Remove(key);
            }
        }

        public Dictionary<string, string> GetDebugInfo()
        {
            var info = new Dictionary<string, string>();
            foreach (var kvp in _pools)
            {
                info.Add(kvp.Value.PrefabName, kvp.Value.GetStatus());
            }
            return info;
        }

        private Pool CreatePool(PoolConfig config)
        {
            int key = config.Prefab.GetInstanceID();
            if (_pools.ContainsKey(key)) return _pools[key];

            var poolGroup = new GameObject($"Pool_{config.Prefab.name}");
            poolGroup.transform.SetParent(_root);

            var pool = new Pool(config, poolGroup.transform);
            _pools.Add(key, pool);
            return pool;
        }

        private class Pool
        {
            public string PrefabName => _config.Prefab.name;

            private readonly Stack<GameObject> _inactiveStack = new Stack<GameObject>();
            private readonly HashSet<GameObject> _activeSet = new HashSet<GameObject>();
            private readonly Dictionary<int, IPoolable> _cachedInterfaces = new Dictionary<int, IPoolable>();
            private readonly PoolConfig _config;
            private readonly Transform _poolRoot;
            private int _totalCreated;

            public Pool(PoolConfig config, Transform poolRoot)
            {
                _config = config;
                _poolRoot = poolRoot;
                Expand(config.InitialSize);
            }

            public GameObject Get()
            {
                if (_inactiveStack.Count == 0) Expand(1);
                var obj = _inactiveStack.Pop();
                _activeSet.Add(obj);
                return obj;
            }

            public void Release(GameObject obj)
            {
                if (_activeSet.Contains(obj)) _activeSet.Remove(obj);
                obj.transform.SetParent(_poolRoot);
                _inactiveStack.Push(obj);
            }

            public void Expand(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    GameObject obj = Instantiate(_config.Prefab, _poolRoot);
                    obj.SetActive(false);

                    int id = obj.GetInstanceID();
                    var poolable = obj.GetComponent<IPoolable>();
                    if (poolable != null) _cachedInterfaces.Add(id, poolable);

                    _inactiveStack.Push(obj);
                    _totalCreated++;
                }
            }

            public void TriggerOnSpawn(int instanceId)
            {
                if (_cachedInterfaces.TryGetValue(instanceId, out var p)) p?.OnSpawn();
            }

            public void TriggerOnDespawn(int instanceId)
            {
                if (_cachedInterfaces.TryGetValue(instanceId, out var p)) p?.OnDespawn();
            }

            public GameObject FindActiveInstance(int instanceId)
            {
                return _activeSet.FirstOrDefault(x => x.GetInstanceID() == instanceId);
            }

            public void Clear()
            {
                foreach (var obj in _inactiveStack) if (obj) Destroy(obj);
                foreach (var obj in _activeSet) if (obj) Destroy(obj);
                _inactiveStack.Clear();
                _activeSet.Clear();
                _cachedInterfaces.Clear();
                Destroy(_poolRoot.gameObject);
            }

            public string GetStatus()
            {
                return $"Available: {_inactiveStack.Count} / Active: {_activeSet.Count} / Total: {_totalCreated}";
            }
        }
    }
}