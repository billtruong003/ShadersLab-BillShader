// Path: Assets/Scripts/Player/PlayerCombat.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Tooltip("Điểm neo trung tâm của người chơi, thường là chính transform của Player.")]
    [SerializeField] private Transform centerAnchor;
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

        if (centerAnchor == null)
        {
            centerAnchor = this.transform;
        }

        inventorySystem.OnInventoryChanged += UpdateWeapons;
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

    private void UpdateSingleWeapon(ref ActiveWeapon currentWeapon, WeaponData newData, int anchorIndex)
    {
        bool weaponIsEquipped = currentWeapon != null;
        bool shouldHaveWeapon = newData != null;

        if (weaponIsEquipped && shouldHaveWeapon && currentWeapon.GetWeaponData() == newData)
        {
            return;
        }

        if (shouldHaveWeapon)
        {
            if (weaponIsEquipped)
            {
                Destroy(currentWeapon.gameObject);
            }

            if (newData.weaponPrefab != null)
            {
                Transform idleAnchorToUse = (anchorIndex < weaponIdleAnchors.Count) ? weaponIdleAnchors[anchorIndex] : centerAnchor;

                if (idleAnchorToUse == null)
                {
                    Debug.LogError($"CRITICAL ERROR: Anchor for weapon slot {anchorIndex} is NULL. Assign it in the PlayerCombat component!", this);
                    return;
                }

                // --- THAY ĐỔI QUAN TRỌNG NHẤT ---
                // Luôn tạo vũ khí với vòng xoay mặc định (không xoay) để đảm bảo tính nhất quán.
                // Các vũ khí cần xoay (như ActiveSword) sẽ tự cập nhật vòng xoay của chúng trong Update().
                GameObject weaponInstance = Instantiate(newData.weaponPrefab, idleAnchorToUse.position, Quaternion.identity);
                // ---------------------------------

                currentWeapon = weaponInstance.GetComponent<ActiveWeapon>();

                if (currentWeapon != null)
                {
                    currentWeapon.SetAnchors(idleAnchorToUse, centerAnchor);
                    currentWeapon.Initialize(newData);
                }
                else
                {
                    Debug.LogError($"Prefab for {newData.itemName} is missing a script inheriting from ActiveWeapon!", this);
                    Destroy(weaponInstance);
                }
            }
        }
        else if (weaponIsEquipped)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }
    }
}