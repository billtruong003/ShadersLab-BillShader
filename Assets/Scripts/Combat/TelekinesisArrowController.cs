// Path: Assets/Scripts/Combat/Weapons/TelekinesisArrowController.cs
using UnityEngine;

public class TelekinesisArrowController : ActiveWeapon
{
    [Header("Arrow Control")]
    [SerializeField] private GameObject arrowPrefab;

    // Biến static để đảm bảo chỉ có MỘT mũi tên tồn tại trong toàn bộ game
    private static TelekinesisArrow _activeArrowInstance;

    public override void Initialize(WeaponData data)
    {
        base.Initialize(data);
        ClaimOrSpawnArrow();
    }

    // Quan trọng: Xóa hàm OnDestroy() cũ để controller không phá hủy mũi tên nữa

    protected override void Update()
    {
        base.Update();
        // Cập nhật lại anchor cho mũi tên mỗi frame, phòng trường hợp nó đổi chủ
        if (_activeArrowInstance != null)
        {
            _activeArrowInstance.UpdateOrbitCenter(idleAnchor);
        }

        if (IsReady() && _activeArrowInstance != null && _activeArrowInstance.IsIdle())
        {
            Attack();
        }
    }

    protected override void PerformAttack()
    {
        _activeArrowInstance.StartAttackSequence(weaponData.baseDamage);
    }

    private void ClaimOrSpawnArrow()
    {
        if (_activeArrowInstance == null)
        {
            GameObject arrowInstance = Instantiate(arrowPrefab, idleAnchor.position, idleAnchor.rotation);
            _activeArrowInstance = arrowInstance.GetComponent<TelekinesisArrow>();

            if (_activeArrowInstance != null)
            {
                // Lần đầu tiên, khởi tạo với anchor hiện tại
                _activeArrowInstance.Initialize(idleAnchor);
            }
            else
            {
                Debug.LogError("Arrow prefab is missing the TelekinesisArrow script!");
                Destroy(arrowInstance);
            }
        }
    }
}