// Path: Assets/Scripts/Combat/Weapons/ArcaneBeamCaster.cs
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class ArcaneBeamCaster : ActiveWeapon
{
    private enum CasterState { Idle, Attacking }

    [Header("Core Components")]
    [Tooltip("Object con chứa hình ảnh của vũ khí. Đây là đối tượng sẽ thực hiện chuyển động lơ lửng.")]
    [SerializeField] private Transform casterVisual;
    [Tooltip("Điểm cụ thể trên visual mà tia laser sẽ được bắn ra. Sẽ tự tạo nếu để trống.")]
    [SerializeField] private Transform shootPoint;

    [Header("Beam Settings")]
    [SerializeField] private float beamDuration = 1.5f;
    [SerializeField] private float damageTickRate = 0.2f;
    [SerializeField] private float beamRange = 20f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;
    [SerializeField] private float beamTextureScrollSpeed = 4f;

    [Header("Idle Dynamics")]
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverSpeed = 1.5f;
    [SerializeField] private float orbitRadius = 0.2f;
    [SerializeField] private float orbitSpeed = 1f;
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float idleRotationSpeed = 5f;

    [Header("Aiming")]
    [Tooltip("Tốc độ vũ khí xoay theo mục tiêu khi đang tấn công.")]
    [SerializeField] private float aimingTurnSpeed = 20f;

    private LineRenderer _lineRenderer;
    private Transform _currentTarget;
    private CasterState _currentState;

    private float _attackStateTimer;
    private float _damageTickTimer;
    private GameObject _currentImpactVFX;
    private Vector3 _initialVisualLocalPosition;
    private float _randomTimeOffset;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
        if (casterVisual == null) casterVisual = transform.GetChild(0);
        _initialVisualLocalPosition = casterVisual.localPosition;
    }

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        _randomTimeOffset = Random.Range(0f, 10f);
        EnsureShootPointExists();
        SwitchState(CasterState.Idle);
    }

    protected override void Update()
    {
        base.Update();

        if (_currentState == CasterState.Idle && IsReady())
        {
            Attack();
        }
        else if (_currentState == CasterState.Attacking)
        {
            UpdateAttackingState();
        }
    }

    private void LateUpdate()
    {
        FollowAnchor();
        if (_currentState == CasterState.Idle)
        {
            ApplyIdleMotion();
        }
    }

    private void FollowAnchor()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;

        if (_currentState == CasterState.Idle)
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
            SwitchState(CasterState.Attacking);
        }
    }

    private void SwitchState(CasterState newState)
    {
        if (_currentState == newState) return;

        OnStateExit(_currentState);
        _currentState = newState;
        OnStateEnter(_currentState);
    }

    private void OnStateEnter(CasterState state)
    {
        if (state == CasterState.Attacking)
        {
            _attackStateTimer = beamDuration;
            _damageTickTimer = 0f;
            _lineRenderer.enabled = true;

            if (impactVFX != null)
            {
                _currentImpactVFX = ObjectPoolManager.Instance.Spawn(impactVFX, _currentTarget.position, Quaternion.identity);
            }
        }
    }

    private void OnStateExit(CasterState state)
    {
        if (state == CasterState.Attacking)
        {
            if (_currentImpactVFX != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(_currentImpactVFX);
                _currentImpactVFX = null;
            }
            _lineRenderer.enabled = false;
            _currentTarget = null;
            cooldownTimer = weaponData.cooldown;
        }
    }

    private void UpdateAttackingState()
    {
        _attackStateTimer -= Time.deltaTime;

        if (IsTargetInvalid(_currentTarget) || _attackStateTimer <= 0)
        {
            SwitchState(CasterState.Idle);
            return;
        }

        AimAtTarget();
        UpdateBeamPosition();
        HandleDamageTicks();
    }

    private void AimAtTarget()
    {
        Vector3 direction = _currentTarget.position - transform.position;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * aimingTurnSpeed);
    }

    private void UpdateBeamPosition()
    {
        _lineRenderer.SetPosition(0, shootPoint.position);
        _lineRenderer.SetPosition(1, _currentTarget.position);

        if (_currentImpactVFX != null)
        {
            _currentImpactVFX.transform.position = _currentTarget.position;
        }

        float textureOffset = -Time.time * beamTextureScrollSpeed;
        _lineRenderer.material.mainTextureOffset = new Vector2(textureOffset, 0);
    }

    private void HandleDamageTicks()
    {
        _damageTickTimer -= Time.deltaTime;
        if (_damageTickTimer > 0f) return;

        _damageTickTimer = damageTickRate;
        float damagePerTick = weaponData.baseDamage / (beamDuration / damageTickRate);

        if (_currentTarget.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(damagePerTick, transform.position);
        }
        else if (_currentTarget.TryGetComponent<DummyHealth>(out var dummyHealth))
        {
            dummyHealth.TakeDamage(damagePerTick, transform.position);
        }
    }

    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, beamRange, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
    }

    private bool IsTargetInvalid(Transform target)
    {
        return target == null || !target.gameObject.activeInHierarchy;
    }

    private void EnsureShootPointExists()
    {
        if (shootPoint == null)
        {
            GameObject sp = new GameObject("AutoShootPoint");
            sp.transform.SetParent(casterVisual, false);
            sp.transform.localPosition = Vector3.forward * 0.5f;
            shootPoint = sp.transform;
        }
    }
}