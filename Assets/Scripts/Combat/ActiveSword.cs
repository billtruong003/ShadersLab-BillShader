// Path: Assets/Scripts/Combat/Weapons/ActiveSword.cs
using UnityEngine;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;

public class ActiveSword : ActiveWeapon
{
    [System.Serializable]
    public struct AttackPattern
    {
        [Header("Animation")]
        public string patternName;
        public Vector3 startRotation;
        public Vector3 endRotation;
        public float swingDuration;

        [Header("Visual Effect (VFX)")]
        public GameObject slashVFXPrefab;
        public Vector3 vfxRotationOffset;
        public Vector3 vfxScaleMultiplier;
    }

    private enum SwordState { Idle, Attacking, Returning }
    private enum AttackSubState { None, Approaching, Swinging }

    [Header("Sword Visuals")]
    [SerializeField] private Transform swordPivot;
    [SerializeField] private GameObject swordVisual;

    [Header("Attack Patterns")]
    [SerializeField] private List<AttackPattern> attackPatterns;

    [Header("Core Mechanics")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float attackSearchRadius = 10f;
    [SerializeField] private float maxAttackLeashDistance = 15f;
    [SerializeField] private float returnProximityThreshold = 1.5f;
    [SerializeField] private float attackDamageArc = 120f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Dynamic Attack")]
    [Tooltip("Tốc độ kiếm bay tới mục tiêu.")]
    [SerializeField] private float attackApproachSpeed = 25f;
    [Tooltip("Khoảng cách kiếm phải đạt được trước khi bắt đầu chém.")]
    [SerializeField] private float attackRange = 2f;


    [Header("Idle Dynamics")]
    [SerializeField] private Vector3 idleLocalRotation = new Vector3(0, 0, 45);
    [SerializeField] private float idleSpinSpeed = 180f;
    [SerializeField] private Vector3 idleSpinAxis = Vector3.up;
    [SerializeField] private float idleOrbitSpeed = 2f;
    [SerializeField] private float idleOrbitRadius = 0.3f;
    [SerializeField] private float idleHoverAmplitude = 0.1f;
    [SerializeField] private float idleHoverSpeed = 1.5f;

    private SwordState currentState;
    private AttackSubState _attackSubState;
    private Transform _currentTarget;

    private Vector3 followVelocity = Vector3.zero;
    private Sequence activeAttackSequence;
    private float baseFollowSpeed;
    private Vector3 _idleVisualOffset = Vector3.zero;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        swordVisual.SetActive(true);
        baseFollowSpeed = followSpeed;
        SwitchState(SwordState.Idle);
    }

    private void OnDestroy()
    {
        activeAttackSequence?.Kill();
        swordVisual.transform.DOKill();
        swordPivot.DOKill();
    }

    protected override void Update()
    {
        base.Update();
        switch (currentState)
        {
            case SwordState.Idle:
                TryFindingTargetAndAttack();
                break;
            case SwordState.Attacking:
                HandleAttackingState();
                break;
            case SwordState.Returning:
                CheckIfReturned();
                break;
        }
    }

    private void LateUpdate()
    {
        if (currentState == SwordState.Idle || currentState == SwordState.Returning)
        {
            HandleFollowingMovement();
        }

        if (currentState == SwordState.Idle)
        {
            ApplyIdleVisuals();
        }
    }

    private void SwitchState(SwordState newState)
    {
        if (currentState == newState) return;
        OnStateExit(currentState);
        currentState = newState;
        OnStateEnter(newState);
    }

    private void OnStateEnter(SwordState state)
    {
        if (state == SwordState.Returning)
        {
            followSpeed = baseFollowSpeed * 2f;
        }
        else if (state == SwordState.Idle)
        {
            swordPivot.DOKill();
            swordPivot.DOLocalRotate(idleLocalRotation, 0.2f).SetEase(Ease.OutQuad);
        }
        else if (state == SwordState.Attacking)
        {
            _attackSubState = AttackSubState.Approaching;
        }
    }

    private void OnStateExit(SwordState state)
    {
        if (state == SwordState.Returning)
        {
            followSpeed = baseFollowSpeed;
        }
        else if (state == SwordState.Idle)
        {
            swordVisual.transform.DOKill();
            swordVisual.transform.DOLocalMove(Vector3.zero, 0.1f).SetEase(Ease.OutQuad);
        }
        else if (state == SwordState.Attacking)
        {
            _currentTarget = null;
            _attackSubState = AttackSubState.None;
            activeAttackSequence?.Kill();
        }
    }

    private void HandleFollowingMovement()
    {
        if (idleAnchor == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, idleAnchor.position, ref followVelocity, 1 / followSpeed);
        Quaternion targetRotation = Quaternion.LookRotation(idleAnchor.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    private void ApplyIdleVisuals()
    {
        float time = Time.time;
        float orbitAngle = time * idleOrbitSpeed;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * idleOrbitRadius;
        float hoverOffset = Mathf.Sin(time * idleHoverSpeed) * idleHoverAmplitude;
        _idleVisualOffset.Set(orbitOffset.x, hoverOffset, orbitOffset.z);

        swordVisual.transform.localPosition = Vector3.Lerp(swordVisual.transform.localPosition, _idleVisualOffset, Time.deltaTime * 10f);
        swordVisual.transform.Rotate(idleSpinAxis, idleSpinSpeed * Time.deltaTime, Space.Self);
    }

    private void TryFindingTargetAndAttack()
    {
        if (!IsReady() || attackPatterns.Count == 0) return;

        _currentTarget = FindNearestEnemy();
        if (_currentTarget == null) return;

        SwitchState(SwordState.Attacking);
    }

    // Hàm này bị bỏ trống vì logic đã được chuyển vào HandleAttackingState
    protected override void PerformAttack() { }

    private void HandleAttackingState()
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            SwitchState(SwordState.Returning);
            return;
        }

        CheckLeashDistance();

        if (_attackSubState == AttackSubState.Approaching)
        {
            Vector3 targetPosition = _currentTarget.position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            if (distanceToTarget > attackRange)
            {
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, attackApproachSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), rotationSpeed * Time.deltaTime);
            }
            else
            {
                BeginSwingPhase();
            }
        }
    }

    private void BeginSwingPhase()
    {
        _attackSubState = AttackSubState.Swinging;

        AttackPattern chosenPattern = attackPatterns[Random.Range(0, attackPatterns.Count)];

        Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }

        activeAttackSequence = DOTween.Sequence();
        activeAttackSequence.SetLink(gameObject);
        activeAttackSequence.AppendCallback(() =>
        {
            if (chosenPattern.slashVFXPrefab != null)
            {
                Quaternion vfxRotation = transform.rotation * Quaternion.Euler(chosenPattern.vfxRotationOffset);
                GameObject vfxInstance = ObjectPoolManager.Instance.Spawn(chosenPattern.slashVFXPrefab, transform.position, vfxRotation);
                vfxInstance.transform.localScale = chosenPattern.vfxScaleMultiplier;
            }
        });

        swordPivot.localEulerAngles = chosenPattern.startRotation;
        activeAttackSequence.Append(swordPivot.DOLocalRotate(chosenPattern.endRotation, chosenPattern.swingDuration).SetEase(Ease.OutQuad));

        activeAttackSequence.OnComplete(() =>
        {
            DealDamageInArc(_currentTarget.position);
            cooldownTimer = weaponData.cooldown;
            SwitchState(SwordState.Idle);
        });
    }


    private void CheckLeashDistance()
    {
        if (idleAnchor != null && Vector3.Distance(transform.position, idleAnchor.position) > maxAttackLeashDistance)
        {
            SwitchState(SwordState.Returning);
        }
    }

    private void CheckIfReturned()
    {
        if (idleAnchor != null && Vector3.Distance(transform.position, idleAnchor.position) < returnProximityThreshold)
        {
            SwitchState(SwordState.Idle);
        }
    }

    private void DealDamageInArc(Vector3 attackCenter)
    {
        float effectiveAttackRange = (weaponData != null && weaponData.areaOfEffect > 0) ? weaponData.areaOfEffect : 3.5f;
        Collider[] enemiesHit = Physics.OverlapSphere(attackCenter, effectiveAttackRange, enemyLayer);

        foreach (var enemyCollider in enemiesHit)
        {
            Vector3 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToEnemy) < attackDamageArc / 2)
            {
                // ---- ĐÂY LÀ DÒNG THAY ĐỔI QUAN TRỌNG NHẤT ----
                // Thay vì tìm DummyHealth, giờ chúng ta tìm EnemyHealth
                if (enemyCollider.GetComponent<EnemyHealth>() != null)
                {
                    enemyCollider.GetComponent<EnemyHealth>()?.TakeDamage(weaponData.baseDamage, transform.position);
                }
                else
                {
                    enemyCollider.GetComponent<DummyHealth>()?.TakeDamage(weaponData.baseDamage, transform.position);
                }
            }
        }
    }
    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, attackSearchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Vẽ bán kính tìm kiếm kẻ địch
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackSearchRadius);

        // Vẽ bán kính gây sát thương
        float effectiveAttackRange = (weaponData != null && weaponData.areaOfEffect > 0) ? weaponData.areaOfEffect : 3.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, effectiveAttackRange);

        // Vẽ hình quạt của vùng tấn công
        UnityEditor.Handles.color = new Color(1, 0, 0, 0.2f);
        Vector3 forwardArc = Quaternion.Euler(0, -attackDamageArc / 2, 0) * transform.forward;
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, forwardArc, attackDamageArc, effectiveAttackRange);
    }
#endif
}