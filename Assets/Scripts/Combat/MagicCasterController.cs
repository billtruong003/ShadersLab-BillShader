// Path: Assets/Scripts/Combat/MagicCasterController.cs
using UnityEngine;
using System.Linq;

public class MagicCasterController : ActiveWeapon
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting Configuration")]
    [SerializeField] private float searchRadius = 18f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Firing Pattern")]
    [SerializeField] private int projectileCount = 1;
    [Tooltip("The angle between projectiles if firing more than one.")]
    [SerializeField] private float spreadAngle = 15f;

    private Transform _currentTarget;

    protected override void Update()
    {
        base.Update();
        FollowAnchor();
        if (IsReady())
        {
            Attack();
        }
    }

    private void FollowAnchor()
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

        bool isSingleProjectile = projectileCount <= 1;
        float halfSpread = spreadAngle / 2f;
        float angleStep = isSingleProjectile ? 0 : spreadAngle / (projectileCount - 1);

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = isSingleProjectile ? 0 : -halfSpread + (i * angleStep);
            float fireAngle = baseAngle + angleOffset;
            Quaternion rotation = Quaternion.Euler(0, -fireAngle + 90, 0);

            GameObject projectileInstance = ObjectPoolManager.Instance.Spawn(projectilePrefab, transform.position, rotation);

            if (projectileInstance.TryGetComponent<MagicProjectile>(out var magicProjectile))
            {
                magicProjectile.Initialize(weaponData.baseDamage, _currentTarget);
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .FirstOrDefault()?.transform;
    }

    public void AddProjectile()
    {
        projectileCount++;
    }
}