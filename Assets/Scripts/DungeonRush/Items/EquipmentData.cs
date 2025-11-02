using DungeonRush.Core;
using DungeonRush.Inventories;
using UnityEngine;

namespace DungeonRush.Items
{
    public enum EquipmentSlot
    {
        Weapon,
        Helmet,
        Chest,
        Legs,
        Boots,
        Accessory
    }

    [CreateAssetMenu(fileName = "NewEquipmentData", menuName = "DungeonRush/Items/Equipment")]
    public class EquipmentData : ItemData
    {
        [Header("Equipment Info")]
        public EquipmentSlot equipmentSlot;
        public DungeonRush.Core.StatModifier[] statModifiers;

        [Header("Weapon Specific")]
        [Tooltip("Gán WeaponData nếu trang bị này là một vũ khí.")]
        public WeaponData weaponData;

        public void Equip(GameObject user, int inventorySlotIndex)
        {
            var equipmentManager = user.GetComponent<EquipmentManager>();
            equipmentManager?.Equip(this, inventorySlotIndex);
        }

        public override void Use(GameObject user)
        {
            Debug.LogWarning("Equipment should be equipped via a specific inventory slot. Direct Use call is not supported.");
        }
    }
}