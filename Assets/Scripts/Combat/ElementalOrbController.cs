// Path: Assets/Scripts/Combat/Weapons/ElementalOrbController.cs
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class ElementalOrbController : ActiveWeapon
{
    private enum OrbState { Idle, Attacking, Returning }

    [Header("Core Visuals")]
    [SerializeField] private Transform orbVisual;

    [Header("Chain Attack Logic")]
    [Tooltip("Số mục tiêu tối đa quả cầu sẽ tấn công trong một chuỗi.")]
    [SerializeField] private int maxTargetsInChain = 3;
    [Tooltip("Tốc độ bay của quả cầu khi tấn công.")]
    [SerializeField] private float travelSpeed = 40f;
    [Tooltip("Bán kính tìm kiếm mục tiêu ban đầu.")]
    [SerializeField] private float initialSearchRadius = 30f;
    [Tooltip("Bán kính tìm kiếm các mục tiêu tiếp theo trong chuỗi.")]
    [SerializeField] private float bounceSearchRadius = 15f;

    [Header("Detonation")]
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject explosionVFX;

    [Header("Idle Dynamics ('Sentient Eye')")]
    [Tooltip("Tốc độ quả cầu xoay để nhìn theo mục tiêu khi ở trạng thái nghỉ.")]
    [SerializeField] private float idleLookSlerpSpeed = 3f;
    [Tooltip("Tần suất (giây) quả cầu tìm một mục tiêu mới để 'nhìn'.")]
    [SerializeField] private float idleLookRecalculateTime = 1.5f;
    [SerializeField] private float idleSpinSpeed = 90f;
    [SerializeField] private Vector3 idleSpinAxis = Vector3.up;
    [SerializeField] private float idleHoverAmplitude = 0.15f;
    [SerializeField] private float idleHoverSpeed = 2f;

    private OrbState _currentState = OrbState.Idle;
    private readonly List<Transform> _attackTargets = new List<Transform>();
    private int _currentTargetIndex;

    private Transform _idleLookTarget;
    private float _idleLookTimer;
    private float _randomTimeOffset;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        _randomTimeOffset = Random.Range(0f, 10f);
        SwitchState(OrbState.Idle);
    }

    protected override void Update()
    {
        base.Update();

        switch (_currentState)
        {
            case OrbState.Idle:
                HandleIdleState();
                break;
            case OrbState.Attacking:
                HandleAttackingState();
                break;
            case OrbState.Returning:
                HandleReturningState();
                break;
        }
    }

    private void LateUpdate()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;

        if (_currentState == OrbState.Idle)
        {
            ApplyIdleVisuals();
        }
    }

    private void SwitchState(OrbState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        if (_currentState == OrbState.Idle)
        {
            orbVisual.SetParent(transform);
            orbVisual.localPosition = Vector3.zero;
        }
    }

    private void HandleIdleState()
    {
        if (IsReady())
        {
            Attack();
        }

        _idleLookTimer -= Time.deltaTime;
        if (_idleLookTimer <= 0)
        {
            FindNewLookTarget();
            _idleLookTimer = idleLookRecalculateTime;
        }
    }

    private void HandleAttackingState()
    {
        Transform currentTarget = GetCurrentTarget();
        if (IsTargetInvalid(currentTarget))
        {
            AdvanceToNextTarget();
            return;
        }

        Vector3 targetPosition = currentTarget.position;
        orbVisual.position = Vector3.MoveTowards(orbVisual.position, targetPosition, travelSpeed * Time.deltaTime);

        if (Vector3.Distance(orbVisual.position, targetPosition) < 0.2f)
        {
            Detonate(targetPosition);
            AdvanceToNextTarget();
        }
    }

    private void HandleReturningState()
    {
        orbVisual.position = Vector3.MoveTowards(orbVisual.position, transform.position, travelSpeed * 1.5f * Time.deltaTime);
        if (Vector3.Distance(orbVisual.position, transform.position) < 0.2f)
        {
            SwitchState(OrbState.Idle);
        }
    }

    private void ApplyIdleVisuals()
    {
        float time = Time.time + _randomTimeOffset;
        float hoverY = Mathf.Sin(time * idleHoverSpeed) * idleHoverAmplitude;
        orbVisual.localPosition = new Vector3(0, hoverY, 0);

        orbVisual.Rotate(idleSpinAxis, idleSpinSpeed * Time.deltaTime, Space.Self);

        if (!IsTargetInvalid(_idleLookTarget))
        {
            Vector3 directionToLook = _idleLookTarget.position - orbVisual.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
            orbVisual.rotation = Quaternion.Slerp(orbVisual.rotation, targetRotation, Time.deltaTime * idleLookSlerpSpeed);
        }
    }

    protected override void PerformAttack()
    {
        FindChainTargets();

        if (_attackTargets.Count > 0)
        {
            _currentTargetIndex = 0;
            orbVisual.SetParent(null, true);
            SwitchState(OrbState.Attacking);
        }
    }

    private void FindChainTargets()
    {
        _attackTargets.Clear();
        var alreadyChosen = new HashSet<Transform>();

        Transform firstTarget = FindNearestEnemy(transform.position, initialSearchRadius, alreadyChosen);
        if (firstTarget == null) return;

        _attackTargets.Add(firstTarget);
        alreadyChosen.Add(firstTarget);

        Transform lastTarget = firstTarget;
        for (int i = 1; i < maxTargetsInChain; i++)
        {
            Transform nextTarget = FindNearestEnemy(lastTarget.position, bounceSearchRadius, alreadyChosen);
            if (nextTarget == null) break;

            _attackTargets.Add(nextTarget);
            alreadyChosen.Add(nextTarget);
            lastTarget = nextTarget;
        }
    }

    private void AdvanceToNextTarget()
    {
        _currentTargetIndex++;
        if (_currentTargetIndex >= _attackTargets.Count)
        {
            SwitchState(OrbState.Returning);
        }
    }

    private void Detonate(Vector3 explosionCenter)
    {
        if (explosionVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(explosionVFX, explosionCenter, Quaternion.identity);
        }

        Collider[] enemiesHit = Physics.OverlapSphere(explosionCenter, explosionRadius, enemyLayer);
        foreach (var enemyCollider in enemiesHit)
        {
            if (enemyCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamage(weaponData.baseDamage, explosionCenter);
            }
            else if (enemyCollider.TryGetComponent<DummyHealth>(out var dummyHealth))
            {
                dummyHealth.TakeDamage(weaponData.baseDamage, explosionCenter);
            }
        }
    }

    private void FindNewLookTarget()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, initialSearchRadius, enemyLayer);
        if (potentialTargets.Length > 0)
        {
            _idleLookTarget = potentialTargets[Random.Range(0, potentialTargets.Length)].transform;
        }
        else
        {
            _idleLookTarget = null;
        }
    }

    private Transform FindNearestEnemy(Vector3 origin, float radius, HashSet<Transform> exclusions)
    {
        return Physics.OverlapSphere(origin, radius, enemyLayer)
            .Where(c => !exclusions.Contains(c.transform))
            .OrderBy(c => Vector3.SqrMagnitude(origin - c.transform.position))
            .FirstOrDefault()?.transform;
    }

    private Transform GetCurrentTarget()
    {
        if (_currentTargetIndex < _attackTargets.Count)
        {
            return _attackTargets[_currentTargetIndex];
        }
        return null;
    }

    private bool IsTargetInvalid(Transform target)
    {
        return target == null || !target.gameObject.activeInHierarchy;
    }
}