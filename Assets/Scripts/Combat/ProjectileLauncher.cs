// Path: Assets/Scripts/Combat/Weapons/ProjectileLauncher.cs
using UnityEngine;
using System.Linq;

public class ProjectileLauncher : ActiveWeapon
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private float searchRadius = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Firing Logic")]
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 10f; // Góc giữa các viên đạn nếu bắn nhiều hơn 1

    private Transform _currentTarget;

    protected override void Update()
    {
        base.Update();
        HandleFollowingMovement();
        if (IsReady())
        {
            Attack();
        }
    }

    private void HandleFollowingMovement()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;
        transform.rotation = idleAnchor.rotation;
    }

    protected override void PerformAttack()
    {
        _currentTarget = FindNearestEnemy();
        if (_currentTarget == null) return;

        Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(directionToTarget.z, directionToTarget.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = (projectileCount > 1) ? (-spreadAngle / 2) + (i * (spreadAngle / (projectileCount - 1))) : 0;
            float fireAngle = baseAngle + angleOffset;

            Quaternion rotation = Quaternion.Euler(0, -fireAngle + 90, 0);

            GameObject projectileInstance = ObjectPoolManager.Instance.Spawn(projectilePrefab, transform.position, rotation);

            if (projectileInstance.TryGetComponent<HomingProjectile>(out var homingProjectile))
            {
                homingProjectile.Initialize(weaponData.baseDamage, _currentTarget);
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        return enemies.OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
                      .FirstOrDefault()?.transform;
    }

    public void AddProjectile()
    {
        projectileCount++;
    }
}