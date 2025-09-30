// Path: Assets/Scripts/Combat/Projectiles/TelekinesisArrow.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum ArrowState { Idle, Attacking, Returning }

[RequireComponent(typeof(TrailRenderer))]
public class TelekinesisArrow : MonoBehaviour
{
    [Header("Idle Behavior")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 360f;

    [Header("Attack Behavior")]
    [SerializeField] private float attackSpeed = 50f;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private float nextTargetSearchRadius = 15f;
    [SerializeField] private int maxPierces = 3;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Idle Behavior")]
    [SerializeField] private float idleHoverAmplitude = 0.2f;
    [SerializeField] private float idleHoverSpeed = 2f;

    private Transform _orbitCenter;
    private ArrowState _currentState;
    private List<Transform> _targets = new List<Transform>();
    private int _currentTargetIndex;
    private float _damage;
    private TrailRenderer _trail;
    private HashSet<int> _hitTargetsThisSequence = new HashSet<int>();
    private float _timeOffset;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _timeOffset = Random.Range(0f, 10f);
    }

    public void Initialize(Transform center)
    {
        _orbitCenter = center;
        SwitchState(ArrowState.Idle);
    }

    private void Update()
    {
        switch (_currentState)
        {
            case ArrowState.Idle:
                HandleIdleOrbit();
                break;
            case ArrowState.Attacking:
                HandleAttackMovement();
                break;
            case ArrowState.Returning:
                HandleReturnMovement();
                break;
        }
    }

    public bool IsIdle() => _currentState == ArrowState.Idle;

    public void StartAttackSequence(float attackDamage)
    {
        _damage = attackDamage;
        FindAttackTargets();

        if (_targets.Count > 0)
        {
            SwitchState(ArrowState.Attacking);
        }
    }

    private void SwitchState(ArrowState newState)
    {
        _currentState = newState;
        if (_currentState == ArrowState.Attacking)
        {
            _trail.emitting = true;
            _currentTargetIndex = 0;
            _hitTargetsThisSequence.Clear();
        }
        else
        {
            _trail.emitting = false;
        }
    }

    private void HandleIdleOrbit()
    {
        if (_orbitCenter == null) return;

        // --- LOGIC MỚI ---
        // 1. Tính toán quỹ đạo tròn trên mặt phẳng XZ
        Quaternion orbitRotation = Quaternion.AngleAxis(orbitSpeed * Time.deltaTime, Vector3.up);
        Vector3 horizontalPosition = transform.position - _orbitCenter.position;
        horizontalPosition.y = 0; // Đảm bảo nó nằm trên mặt phẳng
        horizontalPosition = orbitRotation * horizontalPosition.normalized * orbitRadius;

        // 2. Tính toán lơ lửng lên xuống (bobbing) trên trục Y
        float hoverOffset = Mathf.Sin((Time.time + _timeOffset) * idleHoverSpeed) * idleHoverAmplitude;

        // 3. Kết hợp lại và gán vị trí cuối cùng
        transform.position = _orbitCenter.position + horizontalPosition + new Vector3(0, hoverOffset, 0);
        // --- KẾT THÚC LOGIC MỚI ---

        // Xoay mũi tên để nó luôn hướng về phía trước theo hướng di chuyển
        transform.LookAt(transform.position + (orbitRotation * horizontalPosition));
    }

    private void HandleAttackMovement()
    {
        if (_currentTargetIndex >= _targets.Count)
        {
            SwitchState(ArrowState.Returning);
            return;
        }

        Transform currentTarget = _targets[_currentTargetIndex];
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            _currentTargetIndex++;
            return;
        }

        Vector3 targetPosition = currentTarget.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, attackSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            _currentTargetIndex++;
        }
    }

    private void HandleReturnMovement()
    {
        if (_orbitCenter == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 returnPosition = _orbitCenter.position;
        transform.position = Vector3.MoveTowards(transform.position, returnPosition, attackSpeed * 0.75f * Time.deltaTime);

        if (Vector3.Distance(transform.position, returnPosition) < orbitRadius)
        {
            SwitchState(ArrowState.Idle);
        }
    }

    private void FindAttackTargets()
    {
        _targets.Clear();
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);

        Transform firstTarget = nearbyEnemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .FirstOrDefault()?.transform;

        if (firstTarget == null) return;

        _targets.Add(firstTarget);
        Transform lastTarget = firstTarget;

        for (int i = 0; i < maxPierces - 1; i++)
        {
            Transform nextTarget = Physics.OverlapSphere(lastTarget.position, nextTargetSearchRadius, enemyLayer)
                .Where(e => !_targets.Contains(e.transform))
                .OrderBy(e => Vector3.Distance(lastTarget.position, e.transform.position))
                .FirstOrDefault()?.transform;

            if (nextTarget != null)
            {
                _targets.Add(nextTarget);
                lastTarget = nextTarget;
            }
            else
            {
                break;
            }
        }
    }

    public void UpdateOrbitCenter(Transform center)
    {
        _orbitCenter = center;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_currentState != ArrowState.Attacking) return;
        if (_hitTargetsThisSequence.Contains(other.gameObject.GetInstanceID())) return;

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
            _hitTargetsThisSequence.Add(other.gameObject.GetInstanceID());
        }
    }
}