// Path: Assets/Scripts/Combat/Weapons/TelekinesisArrowController.cs
using UnityEngine;
using System.Linq;

public class TelekinesisArrowController : ActiveWeapon
{
    [Header("Arrow Control")]
    [SerializeField] private GameObject arrowPrefab;

    [Header("Tactical Analysis")]
    [Tooltip("Số lượng kẻ địch tối thiểu để kích hoạt chế độ xuyên phá (Pierce).")]
    [SerializeField] private int pierceActivationThreshold = 3;
    [SerializeField] private float enemyScanRadius = 25f;
    [SerializeField] private LayerMask enemyLayer;

    private static TelekinesisArrow _activeArrowInstance;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        ClaimOrSpawnArrow();
    }

    protected override void Update()
    {
        base.Update();
        EnsureArrowOwnership();

        if (CanAttack())
        {
            Attack();
        }
    }

    protected override void PerformAttack()
    {
        TelekinesisArrow.AttackPattern chosenPattern = ChooseAttackPattern();
        _activeArrowInstance.StartAttackSequence(weaponData.baseDamage, chosenPattern);
    }

    private void EnsureArrowOwnership()
    {
        if (_activeArrowInstance != null)
        {
            _activeArrowInstance.UpdateOrbitCenter(idleAnchor);
        }
    }

    private bool CanAttack()
    {
        return _activeArrowInstance != null && _activeArrowInstance.IsIdle();
    }

    private TelekinesisArrow.AttackPattern ChooseAttackPattern()
    {
        int enemiesInRadius = Physics.OverlapSphere(transform.position, enemyScanRadius, enemyLayer).Length;

        if (enemiesInRadius >= pierceActivationThreshold)
        {
            return TelekinesisArrow.AttackPattern.Pierce;
        }

        return TelekinesisArrow.AttackPattern.Pinpoint;
    }

    private void ClaimOrSpawnArrow()
    {
        if (_activeArrowInstance != null) return;

        GameObject arrowInstance = Instantiate(arrowPrefab, idleAnchor.position, idleAnchor.rotation);
        _activeArrowInstance = arrowInstance.GetComponent<TelekinesisArrow>();

        if (_activeArrowInstance != null)
        {
            _activeArrowInstance.Initialize(idleAnchor);
        }
        else
        {
            Debug.LogError("Arrow prefab is missing the TelekinesisArrow script!");
            Destroy(arrowInstance);
        }
    }
}