using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData { get; private set; }
    public int quantity { get; private set; }

    public InventorySlot(ItemData source, int amount)
    {
        itemData = source;
        quantity = amount;
    }

    public InventorySlot()
    {
        ClearSlot();
    }

    public void ClearSlot()
    {
        itemData = null;
        quantity = 0;
    }

    public void AssignItem(ItemData source, int amount)
    {
        if (itemData == source)
        {
            AddToStack(amount);
        }
        else
        {
            itemData = source;
            quantity = amount;
        }
    }

    public int GetRoomInStack()
    {
        if (itemData == null) return 0;
        return itemData.maxStackSize - quantity;
    }

    public void AddToStack(int amount)
    {
        quantity += amount;
    }

    public void RemoveFromStack(int amount)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            ClearSlot();
        }
    }
}