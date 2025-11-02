using DungeonRush.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonRush.Inventories
{
    [Serializable]
    public class InventorySlot
    {
        public Items.ItemData Item { get; private set; }
        public int Quantity { get; private set; }

        public InventorySlot(Items.ItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public void AddQuantity(int amount) => Quantity += amount;
        public void RemoveQuantity(int amount) => Quantity -= amount;
    }

    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int inventorySize = 20;
        private readonly List<DungeonRush.Inventories.InventorySlot> slots = new List<Inventories.InventorySlot>();

        public event Action OnInventoryUpdated;
        public IReadOnlyList<Inventories.InventorySlot> Slots => slots;

        private void Awake()
        {
            for (int i = 0; i < inventorySize; i++)
            {
                slots.Add(new Inventories.InventorySlot(null, 0));
            }
        }

        // In Assets/Scripts/DungeonRush/Inventories/InventorySystem.cs

        public bool AddItem(DungeonRush.Items.ItemData itemToAdd, int quantity = 1)
        {
            // THÊM DÒNG NÀY VÀO:
            // Nếu vật phẩm được truyền vào là null, coi như việc "thêm" đã thành công mà không cần làm gì cả.
            if (itemToAdd == null) return true;

            if (itemToAdd.isStackable)
            {
                var existingSlot = slots.FirstOrDefault(slot => slot.Item == itemToAdd);
                if (existingSlot != null)
                {
                    existingSlot.AddQuantity(quantity);
                    OnInventoryUpdated?.Invoke();
                    return true;
                }
            }

            var emptySlotIndex = slots.FindIndex(slot => slot.Item == null);
            if (emptySlotIndex != -1)
            {
                slots[emptySlotIndex] = new InventorySlot(itemToAdd, quantity);
                OnInventoryUpdated?.Invoke();
                return true;
            }

            return false;
        }

        public void RemoveFromSlot(int slotIndex, int quantity = 1)
        {
            if (IsSlotInvalid(slotIndex)) return;

            slots[slotIndex].RemoveQuantity(quantity);
            if (slots[slotIndex].Quantity <= 0)
            {
                slots[slotIndex] = new InventorySlot(null, 0);
            }
            OnInventoryUpdated?.Invoke();
        }

        public void UseItem(int slotIndex)
        {
            if (IsSlotInvalid(slotIndex)) return;

            Items.ItemData itemInSlot = slots[slotIndex].Item;
            itemInSlot.Use(gameObject);

            if (itemInSlot is ConsumableData)
            {
                RemoveFromSlot(slotIndex, 1);
            }
        }

        private bool IsSlotInvalid(int slotIndex)
        {
            return slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].Item == null;
        }
    }
}