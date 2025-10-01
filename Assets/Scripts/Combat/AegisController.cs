// Path: Assets/Scripts/Combat/AegisController.cs
using UnityEngine;
using System.Collections.Generic;

public class AegisController : ActiveWeapon
{
    [Header("Shield Mechanics")]
    [SerializeField] private int maxCharges = 3;
    [Tooltip("Sát thương tối đa một lớp khiên có thể hấp thụ. Vượt quá ngưỡng này sẽ phá vỡ TẤT CẢ các lớp khiên.")]
    [SerializeField] private float damageThresholdPerCharge = 10000f;

    [Header("Visuals & Orbiting")]
    [SerializeField] private GameObject shieldSegmentPrefab;
    [SerializeField] private GameObject protectiveBubbleVFX;
    [SerializeField] private float orbitSpeed = 120f;
    [SerializeField] private float orbitRadius = 1.8f;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private Vector3 shieldRotationOffset = new Vector3(90, 0, 0);

    [Header("Effects")]
    [SerializeField] private GameObject blockImpactVFX;
    [SerializeField] private GameObject shieldShatterVFX;

    private readonly List<Transform> _orbitingShields = new List<Transform>();
    private GameObject _activeBubbleInstance;
    private PlayerHealth _playerHealth;
    private int _currentCharges;
    private float _currentOrbitAngle;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        _playerHealth = centerAnchor.GetComponentInParent<PlayerHealth>();

        if (_playerHealth != null)
        {
            _playerHealth.OnBeforeDamageTaken += HandleDamageIntercept;
        }

        RestoreAllCharges();
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnBeforeDamageTaken -= HandleDamageIntercept;
        }
        CleanupVisuals();
    }

    protected override void Update()
    {
        base.Update();
        FollowCenterAnchor();
        HandleOrbiting();

        if (_currentCharges <= 0 && IsReady())
        {
            Attack();
        }
    }

    protected override void PerformAttack()
    {
        RestoreAllCharges();
    }

    private void HandleDamageIntercept(object sender, DamageEventArgs args)
    {
        if (_currentCharges <= 0 || args.IsBlocked)
        {
            return;
        }

        args.IsBlocked = true;

        if (args.DamageAmount > damageThresholdPerCharge)
        {
            ShatterAllCharges();
        }
        else
        {
            ConsumeCharge();
        }
    }

    private void ConsumeCharge()
    {
        _currentCharges--;
        SpawnEffect(blockImpactVFX, transform.position);
        UpdateShieldVisuals();

        if (_currentCharges <= 0)
        {
            DepleteShields();
        }
    }

    private void ShatterAllCharges()
    {
        _currentCharges = 0;
        SpawnEffect(shieldShatterVFX, transform.position);
        DepleteShields();
    }

    private void DepleteShields()
    {
        CleanupVisuals();
        cooldownTimer = weaponData.cooldown;
    }

    private void RestoreAllCharges()
    {
        CleanupVisuals();
        _currentCharges = maxCharges;
        SpawnShieldVisuals();
    }

    private void FollowCenterAnchor()
    {
        if (centerAnchor == null) return;
        transform.position = centerAnchor.position;
    }

    private void HandleOrbiting()
    {
        if (_orbitingShields.Count == 0) return;

        _currentOrbitAngle += orbitSpeed * Time.deltaTime;
        Quaternion offsetQuaternion = Quaternion.Euler(shieldRotationOffset);

        for (int i = 0; i < _orbitingShields.Count; i++)
        {
            float angle = _currentOrbitAngle + (360f / _orbitingShields.Count) * i;
            Vector3 radialDirection = Quaternion.AngleAxis(angle, orbitAxis.normalized) * Vector3.right;
            Vector3 orbitPosition = radialDirection * orbitRadius;
            _orbitingShields[i].localPosition = orbitPosition;

            Vector3 crossProduct = Vector3.Cross(radialDirection, orbitAxis.normalized);
            Quaternion baseRotation = Quaternion.LookRotation(crossProduct, orbitAxis.normalized);
            _orbitingShields[i].rotation = baseRotation * offsetQuaternion;
        }
    }

    private void SpawnShieldVisuals()
    {
        for (int i = 0; i < _currentCharges; i++)
        {
            GameObject shieldInstance = Instantiate(shieldSegmentPrefab, transform);
            _orbitingShields.Add(shieldInstance.transform);
        }

        if (protectiveBubbleVFX != null)
        {
            _activeBubbleInstance = Instantiate(protectiveBubbleVFX, centerAnchor.position, Quaternion.identity, centerAnchor);
        }
        HandleOrbiting();
    }

    private void UpdateShieldVisuals()
    {
        if (_orbitingShields.Count > 0)
        {
            Transform shieldToRemove = _orbitingShields[0];
            _orbitingShields.RemoveAt(0);
            Destroy(shieldToRemove.gameObject);
        }
    }

    private void CleanupVisuals()
    {
        foreach (Transform shield in _orbitingShields)
        {
            if (shield != null) Destroy(shield.gameObject);
        }
        _orbitingShields.Clear();

        if (_activeBubbleInstance != null)
        {
            Destroy(_activeBubbleInstance);
        }
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab != null)
        {
            ObjectPoolManager.Instance.Spawn(effectPrefab, position, Quaternion.identity);
        }
    }
}