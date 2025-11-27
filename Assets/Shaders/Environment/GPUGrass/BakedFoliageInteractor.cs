using UnityEngine;

namespace OptimizeGrass
{
    [ExecuteAlways]
    public class FoliageInteractor : MonoBehaviour
    {
        [SerializeField] private float _radius = 2.0f;
        [SerializeField] private float _strength = 1.0f;
        [SerializeField] private Color _debugColor = new Color(1f, 0.5f, 0f, 0.3f);

        private Transform _t;
        private static readonly int GlobalInteractorPos = Shader.PropertyToID("_GlobalInteractorPos");
        private static readonly int GlobalInteractorParams = Shader.PropertyToID("_GlobalInteractorParams");

        private void OnEnable() => _t = transform;

        private void Update()
        {
            if (_t == null) _t = transform;
            Shader.SetGlobalVector(GlobalInteractorPos, _t.position);
            Shader.SetGlobalVector(GlobalInteractorParams, new Vector4(_radius, _strength, 0, 0));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _debugColor;
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }
}