using UnityEngine;
using System.Threading.Tasks;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class VFXManager : GenesisSingletonService<VFXManager>
    {
        [Inject] private PoolManager _pool;
        [Inject] private TimerManager _timer;

        private void EnsureDependencies()
        {
            if (_pool == null) _pool = Genesis.Get<PoolManager>();
            if (_timer == null) _timer = Genesis.Get<TimerManager>();
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
            EnsureDependencies();
            if (!prefab) return null;

            GameObject vfx = _pool.Spawn(prefab, position, rotation, parent);
            vfx.transform.localScale = scale;

            float lifetime = GetDuration(vfx);

            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);

            _timer.Register(lifetime, () => _pool.Despawn(vfx));

            return vfx;
        }

        public void PlayAttached(GameObject prefab, Transform target, Vector3 offset, bool followRotation = false)
        {
            EnsureDependencies();
            if (!prefab || !target) return;

            GameObject vfx = _pool.Spawn(prefab);
            vfx.transform.SetParent(target);
            vfx.transform.localPosition = offset;
            if (!followRotation) vfx.transform.rotation = Quaternion.identity;
            vfx.transform.localScale = Vector3.one;

            float lifetime = GetDuration(vfx);
            _timer.Register(lifetime, () => _pool.Despawn(vfx));
        }

        private float GetDuration(GameObject obj)
        {
            var ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                return ps.main.duration + ps.main.startLifetime.constantMax;
            }

            var nested = obj.GetComponentsInChildren<ParticleSystem>();
            if (nested.Length > 0)
            {
                float max = 0;
                foreach (var p in nested)
                {
                    float t = p.main.duration + p.main.startLifetime.constantMax;
                    if (t > max) max = t;
                }
                return max > 0 ? max : 2f;
            }

            return 2f; // Default fallback
        }
    }
}