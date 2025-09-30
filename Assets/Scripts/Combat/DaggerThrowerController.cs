using UnityEngine;
using System.Linq;
using System.Collections.Generic;
public class DaggerThrowerController : ActiveWeapon
{
    [Header("Projectile")]
    [SerializeField] private GameObject daggerPrefab;
    [SerializeField] private float projectileSpeed = 30f;

    [Header("Targeting")]
    [SerializeField] private float searchRadius = 20f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Firing Logic")]
    [SerializeField] private int daggerCount = 1;

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
    }

    protected override void PerformAttack()
    {
        List<Transform> targets = FindNearestEnemies(daggerCount);
        if (targets.Count == 0) return;

        foreach (Transform target in targets)
        {
            GameObject daggerInstance = ObjectPoolManager.Instance.Spawn(daggerPrefab, transform.position, transform.rotation);

            if (daggerInstance.TryGetComponent<DaggerProjectile>(out var daggerProjectile))
            {
                // Thay đổi: Truyền vào cả Transform của mục tiêu
                daggerProjectile.Initialize(weaponData.baseDamage, target, projectileSpeed);
            }
        }
    }

    private List<Transform> FindNearestEnemies(int count)
    {
        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .Take(count)
            .Select(c => c.transform)
            .ToList();
    }

    public void AddDagger()
    {
        daggerCount++;
    }
}