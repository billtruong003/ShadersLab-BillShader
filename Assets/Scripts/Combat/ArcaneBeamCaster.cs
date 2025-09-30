// Path: Assets/Scripts/Combat/Weapons/ArcaneBeamCaster.cs
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class ArcaneBeamCaster : ActiveWeapon
{
    [Header("Beam Settings")]
    [SerializeField] private float beamDuration = 1.5f;
    [SerializeField] private float damageTickRate = 0.2f;
    [SerializeField] private float beamRange = 20f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX; // Prefab hiệu ứng va chạm
    [SerializeField] private float beamTextureScrollSpeed = 4f;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyLayer;

    private LineRenderer _lineRenderer;
    private Transform _currentTarget;
    private float _attackStateTimer;
    private float _damageTickTimer;
    private bool _isAttacking = false;
    private GameObject _currentImpactVFX;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        FollowAnchor();

        if (_isAttacking)
        {
            UpdateAttackingState();
        }
        else if (IsReady())
        {
            Attack();
        }
    }

    private void FollowAnchor()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;
    }

    protected override void PerformAttack()
    {
        _currentTarget = FindNearestEnemy();
        if (_currentTarget != null)
        {
            _isAttacking = true;
            _attackStateTimer = beamDuration;
            _damageTickTimer = 0f;
            _lineRenderer.enabled = true;

            if (impactVFX != null)
            {
                _currentImpactVFX = ObjectPoolManager.Instance.Spawn(impactVFX, _currentTarget.position, Quaternion.identity);
            }
        }
    }

    private void UpdateAttackingState()
    {
        _attackStateTimer -= Time.deltaTime;

        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy || _attackStateTimer <= 0)
        {
            EndAttack();
            return;
        }

        UpdateBeamPosition();
        HandleDamageTicks();
    }

    private void UpdateBeamPosition()
    {
        transform.LookAt(_currentTarget); // Xoay controller về phía mục tiêu
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, _currentTarget.position);

        if (_currentImpactVFX != null)
        {
            _currentImpactVFX.transform.position = _currentTarget.position;
        }

        // Tạo hiệu ứng trail cho tia laser
        float textureOffset = -Time.time * beamTextureScrollSpeed;
        _lineRenderer.material.mainTextureOffset = new Vector2(textureOffset, 0);
    }

    private void HandleDamageTicks()
    {
        _damageTickTimer -= Time.deltaTime;
        if (_damageTickTimer <= 0f)
        {
            float damagePerTick = weaponData.baseDamage / (beamDuration / damageTickRate);

            bool hitSuccess = false;
            if (_currentTarget.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamage(damagePerTick, transform.position);
                hitSuccess = true;
            }
            else if (_currentTarget.TryGetComponent<DummyHealth>(out var dummyHealth))
            {
                dummyHealth.TakeDamage(damagePerTick, transform.position);
                hitSuccess = true;
            }

            _damageTickTimer = damageTickRate;
        }
    }

    private void EndAttack()
    {
        if (_currentImpactVFX != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(_currentImpactVFX);
            _currentImpactVFX = null;
        }

        _isAttacking = false;
        _lineRenderer.enabled = false;
        _currentTarget = null;
        cooldownTimer = weaponData.cooldown;
    }

    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, beamRange, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
    }
}