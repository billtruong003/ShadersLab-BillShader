// Path: Assets/Scripts/Combat/DaggerProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DaggerProjectile : MonoBehaviour, IPoolableObject
{
    [Header("Movement")]
    [SerializeField] private float lifetime = 3f;
    [Tooltip("Tốc độ xoay của dao để bám theo mục tiêu. Càng cao, đường cong càng gắt.")]
    [SerializeField] private float turnSpeed = 25f;

    // --- PHẦN MỚI ---
    [Header("Self-Rotation Visuals (While Orbiting)")]
    [Tooltip("Trục tự xoay của dao khi đang lơ lửng. Vector3.forward tạo hiệu ứng 'mũi khoan'.")]
    [SerializeField] private Vector3 selfSpinAxis = Vector3.forward;
    [Tooltip("Tốc độ dao tự xoay.")]
    [SerializeField] private float selfSpinSpeed = 720f;
    // --- KẾT THÚC PHẦN MỚI ---

    [Header("Effects")]
    [SerializeField] private GameObject impactVFX;

    private float _damage;
    private float _speed;
    private Transform _target;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private float _spawnTime;
    private bool _isLaunched;
    private DissolveOnImpact _dissolveEffect;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _dissolveEffect = GetComponent<DissolveOnImpact>();
    }

    public void Launch(float damage, Transform target, float speed)
    {
        _damage = damage;
        _target = target;
        _speed = speed;
        _spawnTime = Time.time;
        _isLaunched = true;

        _rigidbody.isKinematic = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _collider.enabled = true;

        Vector3 initialDirection = (target != null)
            ? (target.position - transform.position).normalized
            : transform.forward;

        _rigidbody.linearVelocity = initialDirection * _speed;
        transform.rotation = Quaternion.LookRotation(initialDirection);
    }

    public void DeactivatePhysicsForOrbit()
    {
        _rigidbody.isKinematic = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        _collider.enabled = false;
        _isLaunched = false;
    }

    // --- PHƯƠNG THỨC MỚI ---
    // Được gọi bởi Controller mỗi frame khi dao đang ở trạng thái chuẩn bị
    public void UpdateOrbitingVisuals()
    {
        // Xoay quanh trục cục bộ (local axis) của chính con dao
        transform.Rotate(selfSpinAxis, selfSpinSpeed * Time.deltaTime, Space.Self);
    }
    // --- KẾT THÚC PHƯƠNG THỨC MỚI ---

    public void OnObjectSpawn() { }

    public void OnObjectReturn()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _target = null;
        _isLaunched = false;
    }

    private void FixedUpdate()
    {
        if (!_isLaunched) return;

        if (Time.time > _spawnTime + lifetime)
        {
            ProcessHit(transform.position);
            return;
        }

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        _rigidbody.linearVelocity = transform.forward * _speed;
    }

    // (Các hàm OnTriggerEnter và ProcessHit giữ nguyên như cũ, không cần thay đổi)
    private void OnTriggerEnter(Collider other)
    {
        if (!_isLaunched) return;

        bool hitSuccess = false;
        Vector3 hitPosition = other.ClosestPoint(transform.position);

        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(_damage, transform.position);
            hitSuccess = true;
        }
        else if (other.TryGetComponent<DummyHealth>(out var dummyHealth))
        {
            dummyHealth.TakeDamage(_damage, transform.position);
            hitSuccess = true;
        }
        else if (other.gameObject.layer != LayerMask.NameToLayer("Player") && !other.isTrigger)
        {
            hitSuccess = true;
        }

        if (hitSuccess)
        {
            ProcessHit(hitPosition);
        }
    }

    private void ProcessHit(Vector3 hitPosition)
    {
        _isLaunched = false;
        _rigidbody.linearVelocity = Vector3.zero;

        if (impactVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(impactVFX, hitPosition, Quaternion.identity);
        }

        if (_dissolveEffect != null)
        {
            _dissolveEffect.TriggerDissolve();
        }
        else
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}