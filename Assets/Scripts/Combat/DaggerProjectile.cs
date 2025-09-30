
// Path: Assets/Scripts/Combat/DaggerProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DaggerProjectile : MonoBehaviour, IPoolableObject
{
    [Header("Movement")]
    [SerializeField] private float lifetime = 3f;
    [Tooltip("Tốc độ xoay của dao để bám theo mục tiêu. Càng cao, đường cong càng gắt.")]
    [SerializeField] private float turnSpeed = 15f;

    [Header("Effects")]
    [SerializeField] private GameObject impactVFX;

    private float _damage;
    private float _speed;
    private Transform _target;
    private Rigidbody _rigidbody;
    private float _spawnTime;
    private DissolveOnImpact _dissolveEffect;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _dissolveEffect = GetComponent<DissolveOnImpact>();
    }

    public void Initialize(float damage, Transform target, float speed)
    {
        _damage = damage;
        _target = target;
        _speed = speed;
    }

    public void OnObjectSpawn()
    {
        _spawnTime = Time.time;
        // Đặt vận tốc ban đầu hướng về mục tiêu
        if (_target != null)
        {
            Vector3 initialDirection = (_target.position - transform.position).normalized;
            _rigidbody.linearVelocity = initialDirection * _speed;
        }
    }

    public void OnObjectReturn()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _target = null;
    }

    private void FixedUpdate()
    {
        if (Time.time > _spawnTime + lifetime)
        {
            ProcessHit(transform.position); // Vẫn kích hoạt hiệu ứng tan biến khi hết hạn
            return;
        }

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Dùng Slerp để xoay hướng di chuyển một cách mượt mà
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        // Luôn di chuyển về phía trước theo hướng hiện tại
        _rigidbody.linearVelocity = transform.forward * _speed;
    }

    private void OnTriggerEnter(Collider other)
    {
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
        if (impactVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(impactVFX, hitPosition, Quaternion.identity);
        }

        if (_dissolveEffect != null)
        {
            // Vô hiệu hóa mục tiêu để coroutine tan biến không bị gián đoạn bởi logic FixedUpdate
            _target = null;
            _dissolveEffect.TriggerDissolve();
        }
        else
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}