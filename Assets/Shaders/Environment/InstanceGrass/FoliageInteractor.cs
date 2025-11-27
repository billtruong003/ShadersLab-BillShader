using UnityEngine;

[ExecuteAlways]
public class FoliageInteractor : MonoBehaviour
{
    [SerializeField] private float _radius = 2.0f;
    [SerializeField] private Color _debugColor = new Color(1f, 0.5f, 0f, 0.3f);

    private static readonly int GlobalInteractorPos = Shader.PropertyToID("_GlobalInteractorPos");
    private Transform _t;

    private void OnEnable()
    {
        _t = transform;
    }

    private void Update()
    {
        if (_t == null) _t = transform;
        Shader.SetGlobalVector(GlobalInteractorPos, _t.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _debugColor;
        Gizmos.DrawSphere(transform.position, _radius);
        Gizmos.color = new Color(_debugColor.r, _debugColor.g, _debugColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}