// Path: Assets/Scripts/Combat/MagicProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagicProjectile : MonoBehaviour, IPoolableObject
{
    [Header("Core Mechanics")]
    [SerializeField] private float initialSpeed = 18f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float lifetime = 4f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;

    private Transform _target;
    private float _damage;
    private Rigidbody _rigidbody;
    private float _spawnTime;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(float damage, Transform target)
    {
        _damage = damage;
        _target = target;
        _rigidbody.linearVelocity = transform.forward * initialSpeed;
    }

    public void OnObjectSpawn()
    {
        _spawnTime = Time.time;
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
            ReturnToPool();
            return;
        }

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }

        _rigidbody.linearVelocity = transform.forward * initialSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool hitSuccess = false;
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

        if (hitSuccess)
        {
            ProcessHit(other.ClosestPoint(transform.position));
        }
    }

    private void ProcessHit(Vector3 hitPosition)
    {
        if (impactVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(impactVFX, hitPosition, Quaternion.identity);
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
}