using DungeonRush.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonRush.Inventories
{
    [RequireComponent(typeof(InventorySystem))]
    public class EquipmentManager : MonoBehaviour
    {
        private readonly Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
        private InventorySystem inventory;

        public event Action<EquipmentSlot, EquipmentData, EquipmentData> OnEquipmentChanged;
        public event Action<WeaponData> OnWeaponEquipped; // WeaponData or null

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }
        }

        public void Equip(EquipmentData newItem, int inventorySlotIndex)
        {
            if (newItem == null) return;

            EquipmentSlot slot = newItem.equipmentSlot;
            EquipmentData oldItem = equippedItems[slot];

            if (oldItem != null)
            {
                if (!inventory.AddItem(oldItem))
                {
                    Debug.LogWarning($"Inventory is full! Cannot unequip {oldItem.displayName} to equip {newItem.displayName}.");
                    return;
                }
            }

            equippedItems[slot] = newItem;
            inventory.RemoveFromSlot(inventorySlotIndex, 1);
            OnEquipmentChanged?.Invoke(slot, oldItem, newItem);

            if (slot == EquipmentSlot.Weapon)
            {
                OnWeaponEquipped?.Invoke(newItem.weaponData);
            }
        }

        public void Unequip(EquipmentSlot slot)
        {
            EquipmentData oldItem = equippedItems[slot];
            if (oldItem == null) return;

            if (inventory.AddItem(oldItem))
            {
                equippedItems[slot] = null;
                OnEquipmentChanged?.Invoke(slot, oldItem, null);
                if (slot == EquipmentSlot.Weapon)
                {
                    OnWeaponEquipped?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning("Inventory is full! Cannot unequip item.");
            }
        }

        public IReadOnlyDictionary<EquipmentSlot, EquipmentData> GetEquippedItems()
        {
            return equippedItems;
        }
    }
}