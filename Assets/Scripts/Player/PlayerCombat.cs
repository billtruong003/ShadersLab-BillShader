// Path: Assets/Scripts/Player/PlayerCombat.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Tooltip("Danh sách các điểm neo vô hình mà vũ khí sẽ bay theo khi nghỉ.")]
    [SerializeField] private List<Transform> weaponIdleAnchors;
    [SerializeField] private InventorySystem inventorySystem;

    private ActiveWeapon activeWeapon1;
    private ActiveWeapon activeWeapon2;

    private void Start()
    {
        if (inventorySystem == null)
        {
            Debug.LogError("InventorySystem is not assigned in PlayerCombat.", this);
            return;
        }
        inventorySystem.OnInventoryChanged += UpdateWeapons;
        // Gọi lần đầu để trang bị vũ khí có sẵn khi bắt đầu game
        UpdateWeapons();
    }

    private void OnDestroy()
    {
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged -= UpdateWeapons;
        }
    }

    private void UpdateWeapons()
    {
        var weaponDataSlot1 = inventorySystem.GetSlotAt(0)?.itemData as WeaponData;
        var weaponDataSlot2 = inventorySystem.GetSlotAt(1)?.itemData as WeaponData;

        UpdateSingleWeapon(ref activeWeapon1, weaponDataSlot1, 0);
        UpdateSingleWeapon(ref activeWeapon2, weaponDataSlot2, 1);
    }

    /// <summary>
    /// Logic trang bị vũ khí đã được làm lại để tránh hủy/tạo không cần thiết.
    /// </summary>
    private void UpdateSingleWeapon(ref ActiveWeapon currentWeapon, WeaponData newData, int anchorIndex)
    {
        bool weaponIsEquipped = currentWeapon != null;
        bool shouldHaveWeapon = newData != null;

        // Case 1: Đã trang bị vũ khí, và data trong slot vẫn đúng -> Không làm gì cả
        if (weaponIsEquipped && shouldHaveWeapon && currentWeapon.GetWeaponData() == newData)
        {
            return;
        }

        // Case 2: Cần phải có vũ khí (newData không null)
        if (shouldHaveWeapon)
        {
            // Nếu đang cầm vũ khí cũ, hủy nó đi
            if (weaponIsEquipped)
            {
                Destroy(currentWeapon.gameObject);
            }

            // Tạo vũ khí mới
            if (newData.weaponPrefab != null)
            {
                GameObject weaponInstance = Instantiate(newData.weaponPrefab, weaponIdleAnchors[anchorIndex].position, weaponIdleAnchors[anchorIndex].rotation);
                currentWeapon = weaponInstance.GetComponent<ActiveWeapon>();

                if (currentWeapon != null)
                {
                    currentWeapon.Initialize(newData);
                    if (anchorIndex < weaponIdleAnchors.Count)
                    {
                        currentWeapon.SetIdleTarget(weaponIdleAnchors[anchorIndex]);
                    }
                }
                else
                {
                    Debug.LogError($"Prefab for {newData.itemName} is missing a script inheriting from ActiveWeapon!", this);
                    Destroy(weaponInstance);
                }
            }
        }
        // Case 3: Không nên có vũ khí (newData là null), nhưng lại đang cầm một vũ khí
        else if (weaponIsEquipped)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }
    }
}