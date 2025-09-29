using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;

    private List<InventorySlot> inventorySlots;
    public IReadOnlyList<InventorySlot> InventorySlots => inventorySlots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        inventorySlots = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }
    }

    public bool AddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        int quantityLeftToAdd = quantity;
        quantityLeftToAdd = AddToExistingStacks(item, quantityLeftToAdd);
        if (quantityLeftToAdd > 0)
        {
            quantityLeftToAdd = AddToNewStacks(item, quantityLeftToAdd);
        }

        bool success = quantityLeftToAdd == 0;
        if (success)
        {
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    public bool RemoveItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0 || !HasItem(item, quantity)) return false;

        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (quantity <= 0) break;

            InventorySlot slot = inventorySlots[i];
            if (slot.itemData == item)
            {
                int amountToRemove = Mathf.Min(quantity, slot.quantity);
                slot.RemoveFromStack(amountToRemove);
                quantity -= amountToRemove;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventorySlot GetSlotAt(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return null;
        return inventorySlots[index];
    }

    public bool HasItem(ItemData item, int quantity = 1)
    {
        int count = inventorySlots.Where(s => s.itemData == item).Sum(s => s.quantity);
        return count >= quantity;
    }

    public ItemData GetItemAt(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return null;
        return inventorySlots[index].itemData;
    }

    public int GetSize() => inventorySize;

    private int AddToExistingStacks(ItemData item, int quantity)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (quantity <= 0) break;
            InventorySlot slot = inventorySlots[i];
            if (slot.itemData == item && slot.GetRoomInStack() > 0)
            {
                int amountToAdd = Mathf.Min(quantity, slot.GetRoomInStack());
                slot.AddToStack(amountToAdd);
                quantity -= amountToAdd;
            }
        }
        return quantity;
    }

    private int AddToNewStacks(ItemData item, int quantity)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (quantity <= 0) break;
            InventorySlot slot = inventorySlots[i];
            if (slot.itemData == null)
            {
                int amountToAdd = Mathf.Min(quantity, item.maxStackSize);
                slot.AssignItem(item, amountToAdd);
                quantity -= amountToAdd;
            }
        }
        return quantity;
    }
}