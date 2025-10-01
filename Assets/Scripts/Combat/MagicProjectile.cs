// Path: Assets/Scripts/Combat/MagicProjectile.cs
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class MagicProjectile : MonoBehaviour, IPoolableObject
{
    private enum FlightPhase { Launch, Homing }

    [Header("Flight Dynamics")]
    [Tooltip("Thời gian cho giai đoạn phóng ban đầu trước khi bám đuổi.")]
    [SerializeField] private float launchDuration = 0.4f;
    [Tooltip("Tốc độ ban đầu của viên đạn.")]
    [SerializeField] private float initialSpeed = 25f;
    [Tooltip("Độ cong của quỹ đạo trong giai đoạn phóng.")]
    [SerializeField] private float launchCurveIntensity = 8f;
    [Tooltip("Tốc độ viên đạn xoay để bám theo mục tiêu trong giai đoạn Homing.")]
    [SerializeField] private float homingTurnSpeed = 20f;
    [Tooltip("Thời gian tồn tại tối đa của viên đạn.")]
    [SerializeField] private float lifetime = 4f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;
    [Tooltip("Kích thước ban đầu khi được tạo ra.")]
    [SerializeField] private Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
    [Tooltip("Kích thước tối đa sẽ đạt được trong suốt vòng đời.")]
    [SerializeField] private Vector3 endScale = Vector3.one;
    [Tooltip("Hiệu ứng nảy nhẹ khi được spawn.")]
    [SerializeField] private float spawnPunchScale = 0.3f;
    [SerializeField] private float spawnPunchDuration = 0.2f;

    private Transform _target;
    private float _damage;
    private Rigidbody _rigidbody;
    private float _spawnTime;
    private FlightPhase _currentPhase;
    private Vector3 _initialLaunchDirection;
    private Vector3 _initialSideVector;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(float damage, Transform target, Vector3 launchDirection, Vector3 sideVector)
    {
        _damage = damage;
        _target = target;
        _initialLaunchDirection = launchDirection;
        _initialSideVector = sideVector;

        transform.rotation = Quaternion.LookRotation(_initialLaunchDirection);
        _rigidbody.linearVelocity = _initialLaunchDirection * initialSpeed;
    }

    public void OnObjectSpawn()
    {
        _spawnTime = Time.time;
        _currentPhase = FlightPhase.Launch;
        transform.localScale = startScale;
        transform.DOPunchScale(Vector3.one * spawnPunchScale, spawnPunchDuration, 1, 0.5f);
    }

    public void OnObjectReturn()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _target = null;
        transform.DOKill();
    }

    private void Update()
    {
        float lifeProgress = (Time.time - _spawnTime) / lifetime;
        transform.localScale = Vector3.Lerp(startScale, endScale, lifeProgress);
    }

    private void FixedUpdate()
    {
        if (Time.time > _spawnTime + lifetime)
        {
            ReturnToPool();
            return;
        }

        UpdateFlightPhase();

        if (_currentPhase == FlightPhase.Launch)
        {
            ExecuteLaunchPhaseMovement();
        }
        else // Homing Phase
        {
            ExecuteHomingPhaseMovement();
        }

        _rigidbody.linearVelocity = transform.forward * initialSpeed;
    }

    private void UpdateFlightPhase()
    {
        if (_currentPhase == FlightPhase.Launch && Time.time > _spawnTime + launchDuration)
        {
            _currentPhase = FlightPhase.Homing;
        }
    }

    private void ExecuteLaunchPhaseMovement()
    {
        // Thêm một lực vuông góc để tạo đường cong
        Vector3 curveForce = _initialSideVector * launchCurveIntensity;
        _rigidbody.AddForce(curveForce, ForceMode.Acceleration);

        // Hơi hướng về mục tiêu một cách nhẹ nhàng
        if (IsTargetValid())
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, homingTurnSpeed * 0.2f * Time.fixedDeltaTime);
        }
    }

    private void ExecuteHomingPhaseMovement()
    {
        if (IsTargetValid())
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, homingTurnSpeed * Time.fixedDeltaTime);
        }
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

    private bool IsTargetValid()
    {
        return _target != null && _target.gameObject.activeInHierarchy;
    }
}