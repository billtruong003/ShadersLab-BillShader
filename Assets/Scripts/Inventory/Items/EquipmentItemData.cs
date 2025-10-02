// Path: Assets/Scripts/Inventory/Items/EquipmentItemData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Elemental Echoes/Items/Equipment")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Details")]
    public EquipmentSlotType slotType;
    public List<StatModifier> modifiers; // Sử dụng lại struct StatModifier từ UpgradeData

    public override void Use(GameObject user)
    {
        // Logic sử dụng sẽ được quản lý bởi EquipmentSystem thông qua việc double-click trên UI
        Debug.Log($"Attempting to equip {itemName} via Use() method.");
        user.GetComponent<EquipmentSystem>()?.Equip(this);
    }
}