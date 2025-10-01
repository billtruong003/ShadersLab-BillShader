// Path: Assets/Scripts/Combat/MagicCasterController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class MagicCasterController : ActiveWeapon
{
    [System.Serializable]
    public struct CastingMotion
    {
        public string motionName;
        public Vector3 startRotation;
        public Vector3 endRotation;
        public float duration;
        [Range(0f, 1f)] public float fireAtProgress;
    }

    private enum CasterState { Idle, Attacking }

    [Header("Core References")]
    [Tooltip("Object con chứa hình ảnh của vũ khí. Đây sẽ là PIVOT cho chuyển động.")]
    [SerializeField] private Transform casterVisual;
    [Tooltip("Điểm cụ thể trên visual mà projectile sẽ được bắn ra.")]
    [SerializeField] private Transform shootPoint;

    [Header("Idle Hover & Motion")]
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverSpeed = 1.5f;
    [SerializeField] private float orbitRadius = 0.2f;
    [SerializeField] private float orbitSpeed = 1f;
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private float spinSpeed = 90f;

    [Header("Aiming")]
    [Tooltip("Tốc độ vũ khí xoay theo mục tiêu khi đang tấn công.")]
    [SerializeField] private float aimingTurnSpeed = 10f;

    [Header("Casting Motions")]
    [SerializeField] private List<CastingMotion> castingMotions;

    [Header("Targeting & Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float searchRadius = 20f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int projectileCount = 1;
    [Tooltip("Góc tỏa ra của chùm đạn khi được phóng đi.")]
    [SerializeField] private float launchArcAngle = 45f;

    private CasterState currentState = CasterState.Idle;
    private Transform _currentTarget;
    private Sequence _activeAttackSequence;
    private Vector3 _initialVisualLocalPosition;
    private float _randomTimeOffset;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        if (casterVisual != null)
        {
            _initialVisualLocalPosition = casterVisual.localPosition;
        }
        _randomTimeOffset = Random.Range(0f, 10f);
        EnsureShootPointExists();
    }

    protected override void Update()
    {
        base.Update();
        FollowAnchor();

        if (currentState == CasterState.Attacking)
        {
            AimTowardsTarget();
        }

        if (IsReady() && currentState == CasterState.Idle)
        {
            TryToInitiateAttack();
        }
    }

    private void LateUpdate()
    {
        ApplyIdleMotion();
    }

    private void OnDestroy()
    {
        _activeAttackSequence?.Kill();
    }

    private void FollowAnchor()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;
        if (currentState == CasterState.Idle)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, idleAnchor.rotation, Time.deltaTime * 5f);
        }
    }

    private void ApplyIdleMotion()
    {
        if (currentState != CasterState.Idle || casterVisual == null) return;

        float time = Time.time + _randomTimeOffset;
        float hoverY = Mathf.Sin(time * hoverSpeed) * hoverAmplitude;
        float orbitAngle = time * orbitSpeed;
        float orbitX = Mathf.Cos(orbitAngle) * orbitRadius;
        float orbitZ = Mathf.Sin(orbitAngle) * orbitRadius;

        Vector3 localOffset = new Vector3(orbitX, hoverY, orbitZ);
        casterVisual.localPosition = _initialVisualLocalPosition + localOffset;
        casterVisual.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

    private void AimTowardsTarget()
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy) return;

        Vector3 direction = _currentTarget.position - transform.position;
        direction.y = 0;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * aimingTurnSpeed);
    }

    private void TryToInitiateAttack()
    {
        _currentTarget = FindNearestEnemy();
        if (_currentTarget != null)
        {
            Attack();
        }
    }

    protected override void PerformAttack()
    {
        if (currentState != CasterState.Idle || castingMotions.Count == 0) return;

        currentState = CasterState.Attacking;
        CastingMotion chosenMotion = castingMotions[Random.Range(0, castingMotions.Count)];

        _activeAttackSequence = DOTween.Sequence();
        _activeAttackSequence.SetLink(gameObject);

        casterVisual.localPosition = _initialVisualLocalPosition;
        casterVisual.localEulerAngles = chosenMotion.startRotation;

        _activeAttackSequence.Append(
            casterVisual.DOLocalRotate(chosenMotion.endRotation, chosenMotion.duration)
                .SetEase(Ease.InOutSine)
        );

        _activeAttackSequence.InsertCallback(
            chosenMotion.duration * chosenMotion.fireAtProgress,
            FireProjectiles
        );

        _activeAttackSequence.OnComplete(() =>
        {
            casterVisual.DOLocalRotate(_initialVisualLocalPosition, 0.2f).SetEase(Ease.OutQuad);
            currentState = CasterState.Idle;
        });
    }

    private void FireProjectiles()
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy) return;

        Vector3 directionToTarget = (_currentTarget.position - shootPoint.position).normalized;
        bool isSingleProjectile = projectileCount <= 1;
        float halfArc = launchArcAngle / 2f;
        float angleStep = isSingleProjectile ? 0 : launchArcAngle / (projectileCount - 1);

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = isSingleProjectile ? 0 : -halfArc + (i * angleStep);
            Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, transform.up);

            Vector3 launchDirection = rotationOffset * directionToTarget;
            Vector3 sideVector = Quaternion.AngleAxis(90, transform.up) * launchDirection;

            GameObject projectileInstance = ObjectPoolManager.Instance.Spawn(projectilePrefab, shootPoint.position, Quaternion.LookRotation(launchDirection));

            if (projectileInstance.TryGetComponent<MagicProjectile>(out var magicProjectile))
            {
                magicProjectile.Initialize(weaponData.baseDamage, _currentTarget, launchDirection, sideVector);
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
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