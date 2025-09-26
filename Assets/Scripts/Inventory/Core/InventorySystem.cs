using System;
using System.Collections.Generic;
using UnityEngine;

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