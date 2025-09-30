// Path: Assets/Scripts/Combat/Weapons/ElementalOrbController.cs
using UnityEngine;
using System.Linq;

public class ElementalOrbController : ActiveWeapon
{
    private enum OrbState { Idle, Attacking, Returning }

    [Header("Orb Visuals")]
    [SerializeField] private Transform orbVisual;

    [Header("Attack Logic")]
    [SerializeField] private float searchRadius = 30f;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float travelSpeed = 40f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Effects")]
    [SerializeField] private GameObject explosionVFX;

    [Header("Idle Dynamics (Built-in)")]
    [SerializeField] private float idleSpinSpeed = 90f;
    [SerializeField] private Vector3 idleSpinAxis = Vector3.up;
    [SerializeField] private float idleHoverAmplitude = 0.15f;
    [SerializeField] private float idleHoverSpeed = 2f;

    private OrbState _currentState = OrbState.Idle;
    private Vector3 _attackTargetPosition;
    private float _randomTimeOffset; // Để các Orb không lơ lửng đồng bộ

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        // Gán một offset ngẫu nhiên để hiệu ứng trông tự nhiên hơn nếu có nhiều orb
        _randomTimeOffset = Random.Range(0f, 10f);
    }

    protected override void Update()
    {
        base.Update();
        HandleStateTransitions();
    }

    private void LateUpdate()
    {
        // Luôn đi theo Anchor
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;

        // Chỉ áp dụng hiệu ứng idle khi ở đúng trạng thái
        if (_currentState == OrbState.Idle)
        {
            ApplyIdleVisuals();
        }
    }

    private void HandleStateTransitions()
    {
        if (IsReady() && _currentState == OrbState.Idle)
        {
            Attack();
        }

        if (_currentState == OrbState.Attacking)
        {
            orbVisual.position = Vector3.MoveTowards(orbVisual.position, _attackTargetPosition, travelSpeed * Time.deltaTime);
            if (Vector3.Distance(orbVisual.position, _attackTargetPosition) < 0.1f)
            {
                Detonate(_attackTargetPosition);
                SwitchState(OrbState.Returning);
            }
        }
        else if (_currentState == OrbState.Returning)
        {
            orbVisual.position = Vector3.MoveTowards(orbVisual.position, transform.position, travelSpeed * 1.5f * Time.deltaTime);
            if (Vector3.Distance(orbVisual.position, transform.position) < 0.2f)
            {
                SwitchState(OrbState.Idle);
            }
        }
    }

    private void SwitchState(OrbState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        if (_currentState == OrbState.Idle)
        {
            // Gắn lại Orb vào controller để nó đi theo người chơi
            orbVisual.SetParent(transform);
        }
    }

    private void ApplyIdleVisuals()
    {
        // Tính toán vị trí lơ lửng (bobbing)
        float hoverY = Mathf.Sin((Time.time + _randomTimeOffset) * idleHoverSpeed) * idleHoverAmplitude;
        orbVisual.localPosition = new Vector3(0, hoverY, 0);

        // Áp dụng hiệu ứng xoay
        orbVisual.Rotate(idleSpinAxis, idleSpinSpeed * Time.deltaTime, Space.Self);
    }

    protected override void PerformAttack()
    {
        Transform target = FindRandomEnemy();
        if (target == null) return;

        _attackTargetPosition = target.position;

        // Tách Orb ra khỏi sự kiểm soát của controller để nó bay tự do
        orbVisual.SetParent(null, true);
        SwitchState(OrbState.Attacking);
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

    private Transform FindRandomEnemy()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        if (potentialTargets.Length == 0) return null;
        return potentialTargets[Random.Range(0, potentialTargets.Length)].transform;
    }
}