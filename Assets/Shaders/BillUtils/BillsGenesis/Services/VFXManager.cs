using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class VFXManager : GenesisSingletonService<VFXManager>
    {
        [Title("Dependencies")]
        [ShowInInspector, ReadOnly]
        [Inject] private PoolManager _pool;

        [ShowInInspector, ReadOnly]
        [Inject] private TimerManager _timer;

        [Title("Optimization")]
        [ShowInInspector, ReadOnly]
        private readonly Dictionary<int, float> _durationCache = new Dictionary<int, float>();

        [Title("Debug Tools")]
        [BoxGroup("Debug"), SerializeField, AssetsOnly]
        private GameObject _debugVfxPrefab;

        [BoxGroup("Debug"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
        [DisableInEditorMode]
        private void TestPlayVFX()
        {
            if (_debugVfxPrefab) Play(_debugVfxPrefab, transform.position + Vector3.up);
        }

        public override Task InitializeAsync()
        {
            if (_pool == null) _pool = Genesis.Get<PoolManager>();
            if (_timer == null) _timer = Genesis.Get<TimerManager>();
            return Task.CompletedTask;
        }

        public void Play(GameObject prefab, Vector3 position)
        {
            Play(prefab, position, Quaternion.identity, null, Vector3.one);
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Play(prefab, position, rotation, null, Vector3.one);
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            Play(prefab, position, rotation, parent, Vector3.one);
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Play(prefab, position, rotation, null, scale);
        }

        public GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Vector3 scale)
        {
            if (!prefab) return null;

            if (_pool == null) _pool = Genesis.Get<PoolManager>();
            if (_timer == null) _timer = Genesis.Get<TimerManager>();

            GameObject vfxInstance = _pool.Spawn(prefab, position, rotation, parent);
            vfxInstance.transform.localScale = scale;

            if (vfxInstance.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Play(true);
            }

            float duration = GetOrCalculateDuration(prefab, vfxInstance);

            // FIX CS1061: Updated to use Post (or Register alias if preferred, but Post is cleaner)
            _timer.Post(duration, () => _pool.Despawn(vfxInstance));

            return vfxInstance;
        }

        public void PlayAttached(GameObject prefab, Transform target, Vector3 offset, bool followRotation = false)
        {
            if (!prefab || !target) return;

            GameObject vfxInstance = Play(prefab, target.position + offset, followRotation ? target.rotation : Quaternion.identity, target, Vector3.one);
            vfxInstance.transform.localPosition = offset;
        }

        private float GetOrCalculateDuration(GameObject prefab, GameObject instance)
        {
            int prefabId = prefab.GetInstanceID();

            if (_durationCache.TryGetValue(prefabId, out float cachedDuration))
            {
                return cachedDuration;
            }

            float duration = CalculateDuration(instance);
            _durationCache[prefabId] = duration;

            return duration;
        }

        private float CalculateDuration(GameObject instance)
        {
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length == 0) return 2.0f;

            float maxDuration = 0f;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps.emission.enabled)
                {
                    float time = ps.main.duration;
                    if (ps.main.loop)
                    {
                        return time > 0 ? time : 2.0f;
                    }
                    float lifetime = ps.main.startLifetime.constantMax;
                    float total = time + lifetime;
                    if (total > maxDuration) maxDuration = total;
                }
            }

            return maxDuration > 0 ? maxDuration : 2.0f;
        }

        [Button("Clear Cache"), BoxGroup("Optimization")]
        public void ClearDurationCache()
        {
            _durationCache.Clear();
        }
    }
}