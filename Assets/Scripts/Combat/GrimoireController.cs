// Path: Assets/Scripts/Combat/Weapons/GrimoireController.cs
using UnityEngine;
using System.Linq;

public class GrimoireController : ActiveWeapon
{
    private enum GrimoireState { Idle, Charging }

    [Header("Core Visuals")]
    [Tooltip("Object con chứa hình ảnh của cuốn sách.")]
    [SerializeField] private Transform casterVisual;
    [Tooltip("Prefab chứa LineRenderer để tạo hiệu ứng tia năng lượng truyền đi.")]
    [SerializeField] private LineRenderer chargeBeamPrefab;

    [Header("Curse Logic: Channel & Detonate")]
    [Tooltip("Thời gian cuốn sách cần để truyền năng lượng vào mục tiêu trước khi phát nổ.")]
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Effects")]
    [Tooltip("Hiệu ứng xuất hiện trên mục tiêu trong khi bị truyền năng lượng. Sẽ được controller quản lý bật/tắt.")]
    [SerializeField] private GameObject chargeVFX;
    [Tooltip("Hiệu ứng khi lời nguyền phát nổ. Prefab này PHẢI có script 'ReturnToPoolAfterEffect'.")]
    [SerializeField] private GameObject detonationVFX;

    [Header("Idle Dynamics")]
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverSpeed = 1.2f;
    [SerializeField] private float orbitRadius = 0.3f;
    [SerializeField] private float orbitSpeed = 0.8f;
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private float spinSpeed = 45f;
    [SerializeField] private float idleRotationSpeed = 4f;

    [Header("Aiming")]
    [SerializeField] private float aimingTurnSpeed = 15f;

    private GrimoireState _currentState;
    private Transform _currentTarget;

    private float _chargeTimer;
    private GameObject _currentChargeVFX;
    private LineRenderer _currentChargeBeam;

    private Vector3 _initialVisualLocalPosition;
    private float _randomTimeOffset;

    private void Awake()
    {
        if (casterVisual == null) casterVisual = transform.GetChild(0);
        _initialVisualLocalPosition = casterVisual.localPosition;
    }

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        _randomTimeOffset = Random.Range(0f, 10f);
        SwitchState(GrimoireState.Idle);
    }

    protected override void Update()
    {
        base.Update();

        if (_currentState == GrimoireState.Idle && IsReady())
        {
            Attack();
        }
        else if (_currentState == GrimoireState.Charging)
        {
            UpdateChargingState();
        }
    }

    private void LateUpdate()
    {
        FollowAnchor();
        if (_currentState == GrimoireState.Idle)
        {
            ApplyIdleMotion();
        }
    }

    private void OnDestroy()
    {
        CleanupCurrentAttack();
    }

    private void FollowAnchor()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;

        if (_currentState == GrimoireState.Idle)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, idleAnchor.rotation, Time.deltaTime * idleRotationSpeed);
        }
    }

    private void ApplyIdleMotion()
    {
        float time = Time.time + _randomTimeOffset;
        float hoverY = Mathf.Sin(time * hoverSpeed) * hoverAmplitude;
        float orbitAngle = time * orbitSpeed;
        float orbitX = Mathf.Cos(orbitAngle) * orbitRadius;
        float orbitZ = Mathf.Sin(orbitAngle) * orbitRadius;

        Vector3 localOffset = new Vector3(orbitX, hoverY, orbitZ);
        casterVisual.localPosition = _initialVisualLocalPosition + localOffset;
        casterVisual.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

    protected override void PerformAttack()
    {
        _currentTarget = FindNearestEnemy();
        if (_currentTarget != null)
        {
            SwitchState(GrimoireState.Charging);
        }
    }

    private void SwitchState(GrimoireState newState)
    {
        if (_currentState == newState) return;

        OnStateExit(_currentState);
        _currentState = newState;
        OnStateEnter(_currentState);
    }

    private void OnStateEnter(GrimoireState state)
    {
        if (state == GrimoireState.Charging)
        {
            _chargeTimer = chargeDuration;
            if (chargeVFX != null)
            {
                _currentChargeVFX = ObjectPoolManager.Instance.Spawn(chargeVFX, _currentTarget.position, Quaternion.identity);
            }
            if (chargeBeamPrefab != null)
            {
                _currentChargeBeam = Instantiate(chargeBeamPrefab);
            }
        }
    }

    private void OnStateExit(GrimoireState state)
    {
        if (state == GrimoireState.Charging)
        {
            CleanupCurrentAttack();
            cooldownTimer = weaponData.cooldown;
            _currentTarget = null;
        }
    }

    private void UpdateChargingState()
    {
        if (IsTargetInvalid(_currentTarget))
        {
            SwitchState(GrimoireState.Idle);
            return;
        }

        AimAtTarget();
        UpdateChargeVisuals();

        _chargeTimer -= Time.deltaTime;
        if (_chargeTimer <= 0f)
        {
            Detonate(_currentTarget);
            SwitchState(GrimoireState.Idle);
        }
    }

    private void UpdateChargeVisuals()
    {
        if (_currentChargeVFX != null)
        {
            _currentChargeVFX.transform.position = _currentTarget.position;
        }

        if (_currentChargeBeam != null)
        {
            _currentChargeBeam.SetPosition(0, casterVisual.position);
            _currentChargeBeam.SetPosition(1, _currentTarget.position);
        }
    }

    private void Detonate(Transform target)
    {
        if (detonationVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(detonationVFX, target.position, Quaternion.identity);
        }

        if (target.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(weaponData.baseDamage, transform.position);
        }
        else if (target.TryGetComponent<DummyHealth>(out var dummyHealth))
        {
            dummyHealth.TakeDamage(weaponData.baseDamage, transform.position);
        }
    }

    private void CleanupCurrentAttack()
    {
        if (_currentChargeVFX != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(_currentChargeVFX);
            _currentChargeVFX = null;
        }
        if (_currentChargeBeam != null)
        {
            Destroy(_currentChargeBeam.gameObject);
            _currentChargeBeam = null;
        }
    }

    private void AimAtTarget()
    {
        Vector3 direction = _currentTarget.position - transform.position;
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * aimingTurnSpeed);
    }

    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
    }

    private bool IsTargetInvalid(Transform target)
    {
        return target == null || !target.gameObject.activeInHierarchy;
    }
}