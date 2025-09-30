// Path: Assets/Scripts/Combat/Weapons/GrimoireController.cs
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class GrimoireController : ActiveWeapon
{
    // Lớp nội bộ để theo dõi từng lời nguyền
    private class ActiveCurse
    {
        public Transform Target;
        public float Timer;
        public GameObject VisualInstance;
    }

    [Header("Curse Logic")]
    [SerializeField] private int numberOfCurses = 2;
    [SerializeField] private float curseDetonationDelay = 1.5f; // Tăng thời gian mặc định
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Effects")]
    [SerializeField] private GameObject curseVFX;
    [SerializeField] private GameObject detonationVFX;

    private readonly List<ActiveCurse> _activeCurses = new List<ActiveCurse>();

    protected override void Update()
    {
        base.Update();
        FollowAnchor();
        UpdateActiveCurses(); // Luôn cập nhật các lời nguyền

        if (IsReady())
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
        List<Transform> targets = FindRandomEnemies(numberOfCurses);
        foreach (Transform target in targets)
        {
            GameObject visualInstance = ObjectPoolManager.Instance.Spawn(curseVFX, target.position, Quaternion.identity);

            _activeCurses.Add(new ActiveCurse
            {
                Target = target,
                Timer = curseDetonationDelay,
                VisualInstance = visualInstance
            });
        }
    }

    private void UpdateActiveCurses()
    {
        // Duyệt ngược để có thể xóa phần tử an toàn
        for (int i = _activeCurses.Count - 1; i >= 0; i--)
        {
            ActiveCurse curse = _activeCurses[i];

            // Nếu mục tiêu chết, hủy lời nguyền
            if (curse.Target == null || !curse.Target.gameObject.activeInHierarchy)
            {
                ObjectPoolManager.Instance.ReturnToPool(curse.VisualInstance);
                _activeCurses.RemoveAt(i);
                continue;
            }

            // Cập nhật vị trí và bộ đếm
            curse.VisualInstance.transform.position = curse.Target.position;
            curse.Timer -= Time.deltaTime;

            if (curse.Timer <= 0)
            {
                Detonate(curse.Target.position, curse.Target.GetComponent<Collider>());
                ObjectPoolManager.Instance.ReturnToPool(curse.VisualInstance);
                _activeCurses.RemoveAt(i);
            }
        }
    }

    private void Detonate(Vector3 position, Collider targetCollider)
    {
        if (detonationVFX != null)
        {
            ObjectPoolManager.Instance.Spawn(detonationVFX, position, Quaternion.identity);
        }

        if (targetCollider == null) return;

        if (targetCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(weaponData.baseDamage, transform.position);
        }
        else if (targetCollider.TryGetComponent<DummyHealth>(out var dummyHealth))
        {
            dummyHealth.TakeDamage(weaponData.baseDamage, transform.position);
        }
    }

    private List<Transform> FindRandomEnemies(int count)
    {
        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => System.Guid.NewGuid())
            .Take(count)
            .Select(c => c.transform)
            .ToList();
    }
}