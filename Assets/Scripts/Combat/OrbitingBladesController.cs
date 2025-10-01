// Path: Assets/Scripts/Combat/OrbitingBladesController.cs
using UnityEngine;
using System.Collections.Generic;

public class OrbitingBladesController : ActiveWeapon
{
    [Header("Orbit Mechanics")]
    [SerializeField] private GameObject bladePrefab;
    [SerializeField] private float orbitSpeed = 180f;
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [Tooltip("Điều chỉnh xoay cục bộ của từng lưỡi kiếm. (90, 0, 0) sẽ làm kiếm nằm ngang.")]
    [SerializeField] private Vector3 bladeRotationOffset = new Vector3(0, 0, 0);

    [Header("Damage Logic")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float damageCooldownPerTarget = 0.5f;

    [Header("Effects")]
    [Tooltip("Prefab hiệu ứng hình ảnh sẽ được tạo ra khi lưỡi kiếm trúng kẻ địch.")]
    [SerializeField] private GameObject hitVFX;

    [Header("Upgrade Path")]
    [SerializeField] private int numberOfBlades = 1;
    [SerializeField] private float scaleMultiplier = 1f;

    private readonly List<Transform> _blades = new List<Transform>();
    private readonly Dictionary<int, float> _hitTargetCooldowns = new Dictionary<int, float>();
    private readonly List<int> _cooledDownTargets = new List<int>();
    private float _currentOrbitAngle = 0f;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        UpdateBlades();
    }

    protected override void Update()
    {
        base.Update();
        HandleFollowingMovement();
        HandleOrbiting();
        UpdateHitCooldowns();
    }

    private void HandleFollowingMovement()
    {
        if (centerAnchor == null) return;
        transform.position = centerAnchor.position;
    }

    private void HandleOrbiting()
    {
        _currentOrbitAngle += orbitSpeed * Time.deltaTime;
        Quaternion offsetQuaternion = Quaternion.Euler(bladeRotationOffset);

        for (int i = 0; i < _blades.Count; i++)
        {
            float angle = _currentOrbitAngle + (360f / _blades.Count) * i;
            Vector3 radialDirection = Quaternion.AngleAxis(angle, orbitAxis.normalized) * Vector3.right;
            Vector3 orbitPosition = radialDirection * orbitRadius;
            _blades[i].localPosition = orbitPosition;

            Quaternion baseRotation = Quaternion.LookRotation(radialDirection, orbitAxis.normalized);
            _blades[i].rotation = baseRotation * offsetQuaternion;
        }
    }

    private void UpdateHitCooldowns()
    {
        if (_hitTargetCooldowns.Count == 0) return;

        _cooledDownTargets.Clear();
        var cooldownKeys = new List<int>(_hitTargetCooldowns.Keys);

        foreach (int key in cooldownKeys)
        {
            if (Time.time >= _hitTargetCooldowns[key])
            {
                _cooledDownTargets.Add(key);
            }
        }

        foreach (int key in _cooledDownTargets)
        {
            _hitTargetCooldowns.Remove(key);
        }
    }

    protected override void PerformAttack() { }

    private void OnBladeHit(Transform bladeTransform, Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        int targetId = other.gameObject.GetInstanceID();
        if (_hitTargetCooldowns.ContainsKey(targetId)) return;

        _hitTargetCooldowns[targetId] = Time.time + damageCooldownPerTarget;

        bool hitSuccess = false;
        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(weaponData.baseDamage, transform.position);
            hitSuccess = true;
        }
        else if (other.TryGetComponent<DummyHealth>(out var dummyHealth))
        {
            dummyHealth.TakeDamage(weaponData.baseDamage, transform.position);
            hitSuccess = true;
        }

        if (hitSuccess && hitVFX != null)
        {
            Vector3 spawnPosition = other.ClosestPoint(bladeTransform.position);
            Quaternion spawnRotation = Quaternion.LookRotation(Random.onUnitSphere);
            ObjectPoolManager.Instance.Spawn(hitVFX, spawnPosition, spawnRotation);
        }
    }

    private void UpdateBlades()
    {
        while (_blades.Count < numberOfBlades)
        {
            GameObject newBladeInstance = Instantiate(bladePrefab, transform);
            Transform newBladeTransform = newBladeInstance.transform;

            // --- PHẦN SỬA LỖI & TỐI ƯU QUAN TRỌNG NHẤT ---
            newBladeTransform.localPosition = Vector3.zero;
            newBladeTransform.localRotation = Quaternion.identity;
            newBladeTransform.localScale = Vector3.one * scaleMultiplier;
            // ---------------------------------------------

            BladeTrigger trigger = newBladeInstance.AddComponent<BladeTrigger>();
            trigger.OnBladeTriggerEnter += OnBladeHit;
            _blades.Add(newBladeTransform);
        }

        while (_blades.Count > numberOfBlades)
        {
            Transform bladeToRemove = _blades[0];
            _blades.RemoveAt(0);
            Destroy(bladeToRemove.gameObject);
        }

        // Cập nhật lại kích thước cho các lưỡi kiếm cũ nếu scaleMultiplier thay đổi
        foreach (var blade in _blades)
        {
            blade.localScale = Vector3.one * scaleMultiplier;
        }

        HandleOrbiting();
    }

    public void AddBlade()
    {
        numberOfBlades++;
        UpdateBlades();
    }

    public void IncreaseSize(float amount)
    {
        scaleMultiplier += amount;
        UpdateBlades();
    }
}

// Lớp BladeTrigger không có thay đổi
public class BladeTrigger : MonoBehaviour
{
    public System.Action<Transform, Collider> OnBladeTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        OnBladeTriggerEnter?.Invoke(this.transform, other);
    }
}