// Path: Assets/Scripts/Combat/TelekinesisArrow.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class TelekinesisArrow : MonoBehaviour
{
    public enum AttackPattern { Pierce, Pinpoint }
    private enum ArrowState { Idle, Preparing, Attacking, ReEngaging, Returning }

    [Header("Effects")]
    [Tooltip("Prefab hiệu ứng hình ảnh sẽ được tạo ra khi mũi tên va chạm thành công.")]
    [SerializeField] private GameObject hitVFX;

    [Header("Component References")]
    [SerializeField] private Transform arrowVisual;
    [SerializeField] private TrailRenderer trail;

    [Header("Movement & Constraints")]
    [SerializeField] private float minimumHeight = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Vector3 visualRotationOffset = Vector3.zero;
    [SerializeField] private float visualLookAtSpeed = 15f;

    [Header("Idle Behavior")]
    [SerializeField] private float idleOrbitRadius = 2.0f;
    [SerializeField] private float idleOrbitSpeed = 180f;
    [SerializeField] private Vector3 idleOrbitAxis = new Vector3(0.5f, 1f, 0).normalized;
    [SerializeField] private float idleHoverSpeed = 3f;
    [SerializeField] private float idleHoverAmplitude = 0.15f;
    [SerializeField] private float idleMovementDampening = 0.1f;

    [Header("Attack Preparation")]
    [SerializeField] private float prepareDuration = 0.4f;
    [SerializeField] private Vector3 preparePositionOffset = new Vector3(0, 1.2f, 0);
    [SerializeField] private float prepareOffsetRandomness = 0.5f;
    [SerializeField] private float prepareSpinSpeed = 2160f;
    [SerializeField] private float prepareMovementDampening = 0.05f;

    [Header("Attack Behavior")]
    [SerializeField] private float attackSpeed = 60f;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private int maxTargets = 3;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float turnForceMultiplier = 50f;
    [SerializeField] private float maxAttackLeashDistance = 40f; // DÂY BUỘC MỚI
    [SerializeField] private float pinpointReEngageDelay = 0.2f;
    [SerializeField] private float pinpointReEngageDistance = 2.5f;

    private Rigidbody _rigidbody;
    private ArrowState _currentState;
    private AttackPattern _currentAttackPattern;
    private Transform _orbitCenter;
    private float _damage;
    private readonly List<Transform> _targets = new List<Transform>();
    private int _currentTargetIndex;
    private float _stateTimer;
    private float _randomTimeOffset;
    private Vector3 _smoothDampVelocity;
    private Quaternion _visualOffsetQuaternion;

    private void Awake()
    {
        InitializeComponents();
        InitializeParameters();
    }

    private void OnEnable()
    {
        if (trail != null) { trail.emitting = true; trail.Clear(); }
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void OnDisable()
    {
        if (trail != null) trail.emitting = false;
    }

    public void Initialize(Transform center)
    {
        _orbitCenter = center;
        SwitchState(ArrowState.Idle);
    }

    private void Update()
    {
        if (_orbitCenter == null) { Destroy(gameObject); return; }

        HandleStateLogic();
        UpdateVisualTransform();
    }

    private void FixedUpdate()
    {
        if (_orbitCenter == null) return;

        HandleMovement();
        EnforceMinimumHeight();
    }

    public void StartAttackSequence(float attackDamage, AttackPattern pattern)
    {
        if (!IsIdle()) return;
        _damage = attackDamage;
        _currentAttackPattern = pattern;

        FindAttackTargets();
        if (_targets.Count > 0)
        {
            SwitchState(ArrowState.Preparing);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_currentState != ArrowState.Attacking) return;
        if (HasCompletedAttackSequence()) return;

        Transform currentTarget = GetCurrentTarget();
        if (other.transform != currentTarget) return;

        ProcessHit(other);
    }

    private void InitializeComponents()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();

        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void InitializeParameters()
    {
        _randomTimeOffset = Random.Range(0f, 10f);
        _visualOffsetQuaternion = Quaternion.Euler(visualRotationOffset);
    }

    // ===================================================================================
    // HÀM QUAN TRỌNG NHẤT ĐÃ ĐƯỢC SỬA LỖI
    // ===================================================================================
    private void HandleStateLogic()
    {
        _stateTimer -= Time.deltaTime;

        switch (_currentState)
        {
            case ArrowState.Preparing:
                if (_stateTimer <= 0) SwitchState(ArrowState.Attacking);
                break;

            case ArrowState.Attacking:
                HandleAttackingStateValidation();
                break;

            case ArrowState.ReEngaging:
                if (_stateTimer <= 0) SwitchState(ArrowState.Attacking);
                if (IsTargetInvalid(GetCurrentTarget())) SwitchState(ArrowState.Returning);
                break;
        }
    }

    private void HandleAttackingStateValidation()
    {
        Transform currentTarget = GetCurrentTarget();

        // 1. Nếu không còn mục tiêu nào trong danh sách, quay về.
        if (HasCompletedAttackSequence())
        {
            SwitchState(ArrowState.Returning);
            return;
        }

        // 2. Nếu mục tiêu hiện tại không hợp lệ (đã chết), chuyển mục tiêu tiếp theo.
        if (IsTargetInvalid(currentTarget))
        {
            AdvanceToNextTarget();
            return;
        }

        // 3. (SỬA LỖI) Nếu mục tiêu đã ở phía sau, coi như trượt và chuyển mục tiêu.
        Vector3 directionToTarget = currentTarget.position - transform.position;
        if (_rigidbody.linearVelocity.sqrMagnitude > 1f && Vector3.Dot(directionToTarget, _rigidbody.linearVelocity) < 0)
        {
            AdvanceToNextTarget();
            return;
        }

        // 4. (SỬA LỖI) Nếu mũi tên bay quá xa người chơi, buộc phải quay về.
        if (Vector3.SqrMagnitude(transform.position - _orbitCenter.position) > maxAttackLeashDistance * maxAttackLeashDistance)
        {
            SwitchState(ArrowState.Returning);
            return;
        }
    }
    // ===================================================================================
    // KẾT THÚC PHẦN SỬA LỖI
    // ===================================================================================


    private void HandleMovement()
    {
        switch (_currentState)
        {
            case ArrowState.Idle: ExecuteIdleMovement(); break;
            case ArrowState.Preparing: ExecutePreparingMovement(); break;
            case ArrowState.Attacking: ExecuteAttackingMovement(); break;
            case ArrowState.ReEngaging: ExecuteReEngagingMovement(); break;
            case ArrowState.Returning: ExecuteReturningMovement(); break;
        }
    }

    private void SwitchState(ArrowState newState)
    {
        if (_currentState == newState) return;
        OnStateExit(_currentState);
        _currentState = newState;
        OnStateEnter(_currentState);
    }

    private void OnStateEnter(ArrowState state)
    {
        switch (state)
        {
            case ArrowState.Idle:
                if (trail != null) trail.Clear();
                _rigidbody.linearVelocity = Vector3.zero;
                break;
            case ArrowState.Preparing:
                _stateTimer = prepareDuration;
                break;
            case ArrowState.ReEngaging:
                _stateTimer = pinpointReEngageDelay;
                break;
        }
    }

    private void OnStateExit(ArrowState state)
    {
        if (state == ArrowState.Preparing && !IsTargetInvalid(GetCurrentTarget()))
        {
            _rigidbody.linearVelocity = (GetCurrentTarget().position - transform.position).normalized * attackSpeed;
        }
    }

    private void ExecuteIdleMovement()
    {
        (Vector3 idealPosition, Quaternion idealRotation) = CalculateOrbitalTransform(Time.time + _randomTimeOffset);
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, idealPosition, ref _smoothDampVelocity, idleMovementDampening);
        _rigidbody.MovePosition(smoothedPosition);
        transform.rotation = Quaternion.Slerp(transform.rotation, idealRotation, Time.fixedDeltaTime * visualLookAtSpeed);
    }

    private void ExecutePreparingMovement()
    {
        Transform firstTarget = GetCurrentTarget();
        if (IsTargetInvalid(firstTarget))
        {
            SwitchState(ArrowState.Returning); // Không có mục tiêu hợp lệ để chuẩn bị
            return;
        }

        Vector2 randomCircle = Random.insideUnitCircle * prepareOffsetRandomness;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        Vector3 targetPreparePosition = _orbitCenter.TransformPoint(preparePositionOffset + randomOffset);
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPreparePosition, ref _smoothDampVelocity, prepareMovementDampening, 100f);
        _rigidbody.MovePosition(smoothedPosition);
        AimAtTarget(firstTarget, visualLookAtSpeed);
    }

    private void ExecuteAttackingMovement()
    {
        Transform currentTarget = GetCurrentTarget();
        if (IsTargetInvalid(currentTarget)) return;

        Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
        Vector3 desiredVelocity = directionToTarget * attackSpeed;
        Vector3 steeringForce = (desiredVelocity - _rigidbody.linearVelocity) * turnForceMultiplier;
        _rigidbody.AddForce(steeringForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

        if (_rigidbody.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_rigidbody.linearVelocity), Time.fixedDeltaTime * visualLookAtSpeed);
        }
    }

    private void ExecuteReEngagingMovement()
    {
        Transform target = GetCurrentTarget();
        if (IsTargetInvalid(target)) return;

        Vector3 directionAway = (transform.position - target.position).normalized;
        Vector3 retreatPosition = target.position + directionAway * pinpointReEngageDistance;

        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, retreatPosition, ref _smoothDampVelocity, 0.1f, 50f);
        _rigidbody.MovePosition(smoothedPosition);
        AimAtTarget(target, visualLookAtSpeed * 2f);
    }

    private void ExecuteReturningMovement()
    {
        Vector3 directionToReturn = (_orbitCenter.position - transform.position).normalized;
        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, directionToReturn * attackSpeed, Time.fixedDeltaTime * 4f);

        if (_rigidbody.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_rigidbody.linearVelocity), Time.fixedDeltaTime * visualLookAtSpeed);
        }

        if (Vector3.Distance(transform.position, _orbitCenter.position) < idleOrbitRadius + 0.5f)
        {
            SwitchState(ArrowState.Idle);
        }
    }

    private void ProcessHit(Collider hitCollider)
    {
        bool hitSuccess = false;
        if (hitCollider.TryGetComponent<EnemyHealth>(out var enemyHealth)) { enemyHealth.TakeDamage(_damage, transform.position); hitSuccess = true; }
        else if (hitCollider.TryGetComponent<DummyHealth>(out var dummyHealth)) { dummyHealth.TakeDamage(_damage, transform.position); hitSuccess = true; }

        if (!hitSuccess) return;

        // TẠO HIỆU ỨNG VA CHẠM
        if (hitVFX != null)
        {
            Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
            Quaternion hitRotation = Quaternion.LookRotation(-_rigidbody.linearVelocity.normalized);
            ObjectPoolManager.Instance.Spawn(hitVFX, hitPoint, hitRotation);
        }

        // XỬ LÝ LOGIC TẤN CÔNG TIẾP THEO
        if (_currentAttackPattern == AttackPattern.Pinpoint)
        {
            SwitchState(ArrowState.ReEngaging);
        }

        AdvanceToNextTarget();
    }

    private void FindAttackTargets()
    {
        _targets.Clear();
        _currentTargetIndex = 0;

        var potentialTargets = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .Select(e => e.transform)
            .OrderBy(e => Vector3.SqrMagnitude(transform.position - e.transform.position))
            .ToList();

        if (potentialTargets.Count == 0) return;

        switch (_currentAttackPattern)
        {
            case AttackPattern.Pierce:
                _targets.AddRange(potentialTargets.Take(maxTargets));
                break;
            case AttackPattern.Pinpoint:
                Transform singleTarget = potentialTargets.First();
                for (int i = 0; i < maxTargets; i++) _targets.Add(singleTarget);
                break;
        }
    }

    private void EnforceMinimumHeight()
    {
        if (transform.position.y < minimumHeight)
        {
            var pos = transform.position;
            pos.y = minimumHeight;
            _rigidbody.position = pos;
        }
    }

    private (Vector3, Quaternion) CalculateOrbitalTransform(float time)
    {
        Quaternion orbit = Quaternion.AngleAxis(time * idleOrbitSpeed, idleOrbitAxis);
        Vector3 position = _orbitCenter.position + orbit * (Vector3.forward * idleOrbitRadius);
        position.y += Mathf.Sin(time * idleHoverSpeed) * idleHoverAmplitude;

        float nextTime = time + 0.01f;
        Quaternion nextOrbit = Quaternion.AngleAxis(nextTime * idleOrbitSpeed, idleOrbitAxis);
        Vector3 nextPosition = _orbitCenter.position + nextOrbit * (Vector3.forward * idleOrbitRadius);
        nextPosition.y += Mathf.Sin(nextTime * idleHoverSpeed) * idleHoverAmplitude;

        Vector3 tangentDirection = (nextPosition - position).normalized;
        Quaternion rotation = (tangentDirection != Vector3.zero) ? Quaternion.LookRotation(tangentDirection) : transform.rotation;
        return (position, rotation);
    }

    private void UpdateVisualTransform()
    {
        if (arrowVisual == null) return;
        arrowVisual.localPosition = Vector3.zero;

        if (_currentState == ArrowState.Preparing)
        {
            arrowVisual.Rotate(0, 0, prepareSpinSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            arrowVisual.localRotation = Quaternion.Slerp(arrowVisual.localRotation, _visualOffsetQuaternion, Time.deltaTime * 10f);
        }
    }

    private void AimAtTarget(Transform target, float turnSpeed)
    {
        if (IsTargetInvalid(target)) return;
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * turnSpeed);
        }
    }

    private bool IsTargetInvalid(Transform target) => target == null || !target.gameObject.activeInHierarchy;
    private bool HasCompletedAttackSequence() => _currentTargetIndex >= _targets.Count;
    private Transform GetCurrentTarget() => _targets.Count > _currentTargetIndex ? _targets[_currentTargetIndex] : null;
    private void AdvanceToNextTarget() => _currentTargetIndex++;
    public bool IsIdle() => _currentState == ArrowState.Idle;
    public void UpdateOrbitCenter(Transform center) => _orbitCenter = center;
}