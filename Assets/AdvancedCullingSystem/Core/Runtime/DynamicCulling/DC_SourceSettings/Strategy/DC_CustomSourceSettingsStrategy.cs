using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NGS.AdvancedCullingSystem.Dynamic
{
    [Serializable]
    public class DC_CustomSourceSettingsStrategy : IDC_SourceSettingsStrategy
    {
        [field: SerializeField]
        public bool ReadyForCulling { get; private set; }
        public Bounds LocalBounds
        {
            get
            {
                return _localBounds;
            }
            set
            {
                _localBounds = value;
            }
        }

        public DC_CustomTargetEvent OnVisibleEvent
        {
            get
            {
                return _onVisible;
            }
        }
        public DC_CustomTargetEvent OnInvisibleEvent
        {
            get
            {
                return _onInvisible;
            }
        }

        [SerializeField]
        private DC_SourceSettings _context;

        [SerializeField]
        private Bounds _localBounds;

        [SerializeField]
        private DC_CustomTargetEvent _onVisible;

        [SerializeField]
        private DC_CustomTargetEvent _onInvisible;

        [SerializeField]
        private BoxCollider _collider;


        public DC_CustomSourceSettingsStrategy(DC_SourceSettings context)
        {
            _context = context;
            _localBounds = new Bounds(Vector3.zero, Vector3.one);

            _onVisible = new DC_CustomTargetEvent();
            _onInvisible = new DC_CustomTargetEvent();
        }

        public bool CheckCompatibilityAndGetComponents(out string incompatibilityReason)
        {
            incompatibilityReason = "";
            return true;
        }

        public void PrepareForCulling()
        {
            if (ReadyForCulling)
                return;

            GameObject go = new GameObject("DC_Collider");

            go.transform.parent = _context.transform;
            go.layer = _context.CullingLayer;

            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localScale = Vector3.one;

            _collider = go.AddComponent<BoxCollider>();
            _collider.center = _localBounds.center;
            _collider.size = _localBounds.size;

            ReadyForCulling = true;
        }

        public void ClearData()
        {
            if (!ReadyForCulling)
                return;

            if (_collider != null)
                UnityEngine.Object.DestroyImmediate(_collider.gameObject);

            _collider = null;

            ReadyForCulling = false;
        }


        public bool TryGetBounds(ref Bounds bounds)
        {
            bounds.center = _context.transform.position + _localBounds.center;
            bounds.size = _localBounds.size;

            return true;
        }

        public ICullingTarget CreateCullingTarget()
        {
            Bounds bounds = new Bounds();

            TryGetBounds(ref bounds);

            return new DC_CustomTarget(_context.gameObject, bounds, _onVisible, _onInvisible);
        }

        public IEnumerable<Collider> GetColliders()
        {
            if (_collider == null)
                yield break;

            yield return _collider;
        }
    }
}
