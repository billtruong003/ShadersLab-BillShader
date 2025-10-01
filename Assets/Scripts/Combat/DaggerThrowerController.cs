using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DaggerThrowerController : ActiveWeapon
{
    private enum DaggerState { Cooldown, Preparing, Attacking }

    [Header("Core Mechanics")]
    [SerializeField] private GameObject daggerPrefab;
    [SerializeField] private int daggerCount = 3;
    [SerializeField] private float projectileSpeed = 40f;
    [SerializeField] private float preparationDuration = 1.5f;

    [Header("Targeting")]
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private LayerMask enemyLayer;

    // --- PHẦN MỚI ---
    [Header("Collision Avoidance")]
    [Tooltip("Các layer được coi là môi trường (đất, tường,...) để dao né.")]
    [SerializeField] private LayerMask environmentLayer;
    [Tooltip("Khoảng cách dao sẽ lùi lại từ bề mặt va chạm để tránh clipping.")]
    [SerializeField] private float avoidanceOffset = 0.2f;
    // --- KẾT THÚC PHẦN MỚI ---

    [Header("Orbit Visuals")]
    [Tooltip("Trục các con dao sẽ xoay quanh. (0.4, 1, 0) tạo hiệu ứng nghiêng đẹp mắt.")]
    [SerializeField] private Vector3 orbitAxis = new Vector3(0.4f, 1f, 0);
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 270f;
    [Tooltip("Tốc độ các con dao bay đến vị trí lơ lửng của chúng.")]
    [SerializeField] private float daggerFollowSpeed = 20f;
    [Tooltip("Tốc độ các con dao xoay theo hướng của người chơi.")]
    [SerializeField] private float daggerRotationSpeed = 15f;


    [Header("Launch Sequence")]
    [Tooltip("Độ trễ giữa mỗi lần phóng dao trong chuỗi tấn công.")]
    [SerializeField] private float launchDelay = 0.08f;

    private DaggerState currentState;
    private readonly List<DaggerProjectile> preparedDaggers = new List<DaggerProjectile>();

    private Transform _orbitPivot;
    private readonly List<Transform> _orbitSlots = new List<Transform>();
    private Coroutine _launchCoroutine;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        orbitAxis.Normalize();
        SetupOrbitVisuals();
        SwitchState(DaggerState.Cooldown);
    }

    private void SetupOrbitVisuals()
    {
        _orbitPivot = new GameObject("DaggerOrbitPivot").transform;
        _orbitPivot.SetParent(centerAnchor);
        _orbitPivot.localPosition = Vector3.zero;

        for (int i = 0; i < 15; i++)
        {
            GameObject slot = new GameObject($"OrbitSlot_{i}");
            slot.transform.SetParent(_orbitPivot, false);
            _orbitSlots.Add(slot.transform);
        }
    }

    private void OnDestroy()
    {
        ClearPreparedDaggers();
        if (_orbitPivot != null)
        {
            Destroy(_orbitPivot.gameObject);
        }
    }

    protected override void Update()
    {
        base.Update();
        FollowIdleAnchor();

        switch (currentState)
        {
            case DaggerState.Cooldown:
                if (IsReady()) SwitchState(DaggerState.Preparing);
                break;
            case DaggerState.Preparing:
                HandleOrbitingAndAvoidance();
                break;
            case DaggerState.Attacking:
                break;
        }
    }

    private void SwitchState(DaggerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (currentState)
        {
            case DaggerState.Preparing:
                StartCoroutine(PreparationRoutine());
                break;
            case DaggerState.Attacking:
                if (_launchCoroutine != null) StopCoroutine(_launchCoroutine);
                _launchCoroutine = StartCoroutine(LaunchSequence());
                break;
            case DaggerState.Cooldown:
                cooldownTimer = weaponData.cooldown;
                break;
        }
    }

    private void UpdateOrbitSlotPositions()
    {
        for (int i = 0; i < _orbitSlots.Count; i++)
        {
            bool isActive = i < daggerCount;
            _orbitSlots[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                float angle = 360f / daggerCount * i;
                Vector3 radialDirection = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                _orbitSlots[i].localPosition = radialDirection * orbitRadius;
            }
        }
    }

    private void FollowIdleAnchor()
    {
        if (idleAnchor == null) return;
        transform.position = idleAnchor.position;
    }

    private void HandleOrbitingAndAvoidance()
    {
        _orbitPivot.Rotate(orbitAxis, orbitSpeed * Time.deltaTime, Space.World);

        for (int i = 0; i < preparedDaggers.Count; i++)
        {
            DaggerProjectile dagger = preparedDaggers[i];
            if (dagger == null) continue;

            Transform daggerTransform = dagger.transform;
            Transform slotTransform = _orbitSlots[i];

            Vector3 idealPosition = slotTransform.position;
            Vector3 finalTargetPosition = CalculateAvoidedPosition(idealPosition);

            // Cập nhật vị trí và hướng chung của dao (hành vi tập thể)
            daggerTransform.position = Vector3.Lerp(daggerTransform.position, finalTargetPosition, Time.deltaTime * daggerFollowSpeed);
            Quaternion targetRotation = Quaternion.LookRotation(centerAnchor.forward, centerAnchor.up);
            daggerTransform.rotation = Quaternion.Slerp(daggerTransform.rotation, targetRotation, Time.deltaTime * daggerRotationSpeed);

            // --- DÒNG MỚI ĐƯỢC THÊM VÀO ---
            // Ra lệnh cho dao tự thực hiện hành vi cá nhân của nó
            dagger.UpdateOrbitingVisuals();
            // ---------------------------------
        }
    }

    // --- HÀM MỚI QUAN TRỌNG NHẤT ---
    private Vector3 CalculateAvoidedPosition(Vector3 idealPosition)
    {
        Vector3 center = centerAnchor.position;
        Vector3 directionFromCenter = idealPosition - center;
        float distanceToIdeal = directionFromCenter.magnitude;

        // Chỉ thực hiện raycast nếu có layer môi trường được chọn
        if (environmentLayer.value != 0 && Physics.Raycast(center, directionFromCenter.normalized, out RaycastHit hit, distanceToIdeal, environmentLayer))
        {
            // Nếu có va chạm, vị trí mới là điểm va chạm lùi lại một chút
            return hit.point - directionFromCenter.normalized * avoidanceOffset;
        }

        // Nếu không có va chạm, giữ nguyên vị trí lý tưởng
        return idealPosition;
    }

    private IEnumerator PreparationRoutine()
    {
        SpawnDaggersForPreparation();
        yield return new WaitForSeconds(preparationDuration);

        if (currentState == DaggerState.Preparing)
        {
            SwitchState(DaggerState.Attacking);
        }
    }

    private void SpawnDaggersForPreparation()
    {
        ClearPreparedDaggers();
        UpdateOrbitSlotPositions();

        for (int i = 0; i < daggerCount; i++)
        {
            GameObject daggerInstance = ObjectPoolManager.Instance.Spawn(daggerPrefab, _orbitSlots[i].position, Quaternion.identity);
            if (daggerInstance.TryGetComponent<DaggerProjectile>(out var daggerProjectile))
            {
                daggerProjectile.DeactivatePhysicsForOrbit();
                preparedDaggers.Add(daggerProjectile);
            }
        }
    }

    private IEnumerator LaunchSequence()
    {
        List<Transform> targets = FindNearestEnemies(daggerCount);
        int targetIndex = 0;

        var daggersToLaunch = new List<DaggerProjectile>(preparedDaggers);
        preparedDaggers.Clear();

        foreach (var dagger in daggersToLaunch)
        {
            if (dagger != null)
            {
                Transform target = (targets.Count > 0) ? targets[targetIndex % targets.Count] : null;
                dagger.Launch(weaponData.baseDamage, target, projectileSpeed);
                if (targets.Count > 0) targetIndex++;
            }
            if (launchDelay > 0)
            {
                yield return new WaitForSeconds(launchDelay);
            }
        }
        SwitchState(DaggerState.Cooldown);
    }

    private void ClearPreparedDaggers()
    {
        foreach (var dagger in preparedDaggers)
        {
            if (dagger != null && dagger.gameObject.activeInHierarchy)
            {
                ObjectPoolManager.Instance.ReturnToPool(dagger.gameObject);
            }
        }
        preparedDaggers.Clear();
    }

    private List<Transform> FindNearestEnemies(int count)
    {
        if (count <= 0) return new List<Transform>();

        return Physics.OverlapSphere(transform.position, searchRadius, enemyLayer)
            .OrderBy(c => Vector3.SqrMagnitude(transform.position - c.transform.position))
            .Take(count)
            .Select(c => c.transform)
            .ToList();
    }

    protected override void PerformAttack() { }

    public void AddDagger()
    {
        daggerCount++;
    }
}