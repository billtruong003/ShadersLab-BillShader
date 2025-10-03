using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Lớp này không kế thừa từ MonoBehaviour nữa.
// Đây là một lớp C# thuần túy để quản lý logic dữ liệu.
public class InventorySystem
{
    private readonly List<InventorySlot> inventorySlots;
    public IReadOnlyList<InventorySlot> InventorySlots => inventorySlots;

    public event Action OnInventoryChanged;

    // Đây chính là constructor mà GameDataManager đang tìm kiếm.
    public InventorySystem(int size)
    {
        inventorySlots = new List<InventorySlot>(size);
        for (int i = 0; i < size; i++)
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
            InvokeChangeEvent();
        }

        return success;
    }

    public bool RemoveItemAt(int index, int quantity)
    {
        if (!IsIndexValid(index) || quantity <= 0) return false;

        InventorySlot slot = inventorySlots[index];
        if (slot.itemData == null || slot.quantity < quantity) return false;

        slot.RemoveFromStack(quantity);
        InvokeChangeEvent();
        return true;
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

        InvokeChangeEvent();
        return true;
    }

    public InventorySlot GetSlotAt(int index)
    {
        return IsIndexValid(index) ? inventorySlots[index] : null;
    }

    public ItemData GetItemAt(int index)
    {
        return GetSlotAt(index)?.itemData;
    }

    public bool HasItem(ItemData item, int quantity = 1)
    {
        int count = inventorySlots.Where(s => s.itemData == item).Sum(s => s.quantity);
        return count >= quantity;
    }

    public int GetSize() => inventorySlots.Count;

    public void SwapSlots(int indexA, int indexB)
    {
        if (!IsIndexValid(indexA) || !IsIndexValid(indexB)) return;

        InventorySlot temp = new InventorySlot(inventorySlots[indexA]);
        inventorySlots[indexA].UpdateSlot(inventorySlots[indexB]);
        inventorySlots[indexB].UpdateSlot(temp);

        InvokeChangeEvent();
    }

    public void Sort()
    {
        var sortedSlots = inventorySlots
            .Where(s => s.itemData != null)
            .OrderBy(s => s.itemData.itemName)
            .ThenByDescending(s => s.quantity)
            .ToList();

        var emptySlots = inventorySlots
            .Where(s => s.itemData == null)
            .ToList();

        inventorySlots.Clear();
        inventorySlots.AddRange(sortedSlots);
        inventorySlots.AddRange(emptySlots);

        InvokeChangeEvent();
    }

    private bool IsIndexValid(int index)
    {
        return index >= 0 && index < inventorySlots.Count;
    }

    private int AddToExistingStacks(ItemData item, int quantity)
    {
        foreach (var slot in inventorySlots)
        {
            if (quantity <= 0) break;
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
        foreach (var slot in inventorySlots)
        {
            if (quantity <= 0) break;
            if (slot.itemData == null)
            {
                int amountToAdd = Mathf.Min(quantity, item.maxStackSize);
                slot.AssignItem(item, amountToAdd);
                quantity -= amountToAdd;
            }
        }
        return quantity;
    }

    private void InvokeChangeEvent()
    {
        OnInventoryChanged?.Invoke();
    }
}