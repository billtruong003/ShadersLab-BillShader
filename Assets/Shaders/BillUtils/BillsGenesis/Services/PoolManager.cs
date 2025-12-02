using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BillsGenesis.Core;
using Sirenix.OdinInspector;

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
            [Required, AssetsOnly] public GameObject Prefab;
            [MinValue(1)] public int InitialSize = 10;
            [MinValue(10)] public int MaxSize = 100;
            public bool AutoExpand = true;
            public bool AutoTrim = true;
            [ShowIf("AutoTrim"), MinValue(5f)] public float TrimDelay = 60f;
        }

        [Title("Settings")]
        [SerializeField, LabelText("Global Trim Interval")] private float _trimInterval = 30f;

        [Title("Prewarm Configuration")]
        [SerializeField, TableList] private List<PoolConfig> _prewarmConfigs = new List<PoolConfig>();

        private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
        private readonly Dictionary<int, int> _instanceIdToPrefabId = new Dictionary<int, int>();
        private Transform _root;
        private float _lastTrimTime;

        public override Task InitializeAsync()
        {
            _root = new GameObject("Pool_System_Root").transform;
            DontDestroyOnLoad(_root);
            _root.SetParent(transform);

            foreach (var config in _prewarmConfigs)
            {
                if (config.Prefab == null) continue;
                CreatePool(config);
            }

            return Task.CompletedTask;
        }

        public override void OnUpdate()
        {
            if (Time.unscaledTime - _lastTrimTime >= _trimInterval)
            {
                TrimAllPools();
                _lastTrimTime = Time.unscaledTime;
            }
        }

        public GameObject Spawn(GameObject prefab) => Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        public GameObject Spawn(GameObject prefab, Transform parent) => Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) => Spawn(prefab, position, rotation, null);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = CreatePool(new PoolConfig { Prefab = prefab, InitialSize = 1, AutoExpand = true });
            }

            GameObject obj = pool.Get();
            if (obj == null) return null;

            var t = obj.transform;
            t.SetPositionAndRotation(position, rotation);
            if (parent != null) t.SetParent(parent);

            obj.SetActive(true);
            _instanceIdToPrefabId[obj.GetInstanceID()] = key;

            var poolables = obj.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawn();

            return obj;
        }

        public T Spawn<T>(T component) where T : Component
        {
            return Spawn(component.gameObject).GetComponent<T>();
        }

        public T Spawn<T>(T component, Vector3 position, Quaternion rotation) where T : Component
        {
            return Spawn(component.gameObject, position, rotation).GetComponent<T>();
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            int id = obj.GetInstanceID();
            if (!_instanceIdToPrefabId.TryGetValue(id, out int key))
            {
                Destroy(obj);
                return;
            }

            if (_pools.TryGetValue(key, out var pool))
            {
                var poolables = obj.GetComponents<IPoolable>();
                for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawn();

                _instanceIdToPrefabId.Remove(id);
                obj.SetActive(false);
                pool.Release(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        public void Despawn(GameObject obj, float delay)
        {
            if (delay <= 0)
            {
                Despawn(obj);
                return;
            }
            StartCoroutine(DespawnDelayRoutine(obj, delay));
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var pool))
            {
                CreatePool(new PoolConfig { Prefab = prefab, InitialSize = count });
            }
            else
            {
                pool.EnsureCapacity(count);
            }
        }

        public void ClearPool(GameObject prefab)
        {
            if (prefab == null) return;
            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Clear();
                _pools.Remove(key);
            }
        }

        public Dictionary<string, string> GetDebugInfo()
        {
            var dict = new Dictionary<string, string>();
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                string name = pool.Config.Prefab ? pool.Config.Prefab.name : $"ID:{kvp.Key}";
                dict[name] = $"Active: {pool.ActiveCount} | Cached: {pool.InactiveCount} | Max: {pool.Config.MaxSize}";
            }
            return dict;
        }

        private Pool CreatePool(PoolConfig config)
        {
            int key = config.Prefab.GetInstanceID();
            if (_pools.ContainsKey(key)) return _pools[key];

            var poolObj = new GameObject($"Pool_{config.Prefab.name}");
            poolObj.transform.SetParent(_root);

            var pool = new Pool(config, poolObj.transform);
            _pools.Add(key, pool);
            return pool;
        }

        private void TrimAllPools()
        {
            foreach (var pool in _pools.Values) pool.Trim();
        }

        private IEnumerator DespawnDelayRoutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeSelf) Despawn(obj);
        }

        private class Pool
        {
            public int ActiveCount { get; private set; }
            public int InactiveCount => _stack.Count;
            public PoolConfig Config => _config;

            private readonly Stack<GameObject> _stack = new Stack<GameObject>();
            private readonly PoolConfig _config;
            private readonly Transform _root;
            private float _lastReleaseTime;

            public Pool(PoolConfig config, Transform root)
            {
                _config = config;
                _root = root;
                EnsureCapacity(config.InitialSize);
            }

            public GameObject Get()
            {
                GameObject obj = null;
                while (_stack.Count > 0)
                {
                    obj = _stack.Pop();
                    if (obj != null) break;
                }

                if (obj == null)
                {
                    if (_config.AutoExpand || ActiveCount < _config.MaxSize)
                    {
                        obj = CreateNew();
                    }
                }

                if (obj != null) ActiveCount++;
                return obj;
            }

            public void Release(GameObject obj)
            {
                obj.transform.SetParent(_root);
                _stack.Push(obj);
                ActiveCount--;
                _lastReleaseTime = Time.unscaledTime;
            }

            public void EnsureCapacity(int count)
            {
                int current = _stack.Count + ActiveCount;
                int needed = count - current;
                for (int i = 0; i < needed; i++)
                {
                    var obj = CreateNew();
                    obj.SetActive(false);
                    _stack.Push(obj);
                }
            }

            public void Trim()
            {
                if (!_config.AutoTrim || _stack.Count <= _config.InitialSize) return;
                if (Time.unscaledTime - _lastReleaseTime < _config.TrimDelay) return;

                while (_stack.Count > _config.InitialSize)
                {
                    var obj = _stack.Pop();
                    if (obj != null) UnityEngine.Object.Destroy(obj);
                }
            }

            public void Clear()
            {
                while (_stack.Count > 0)
                {
                    var obj = _stack.Pop();
                    if (obj != null) UnityEngine.Object.Destroy(obj);
                }
                ActiveCount = 0;
            }

            private GameObject CreateNew()
            {
                return UnityEngine.Object.Instantiate(_config.Prefab, _root);
            }
        }
    }
}