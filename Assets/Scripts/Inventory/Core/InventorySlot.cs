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

    // Copy constructor
    public InventorySlot(InventorySlot source)
    {
        itemData = source.itemData;
        quantity = source.quantity;
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

    public void UpdateSlot(InventorySlot source)
    {
        itemData = source.itemData;
        quantity = source.quantity;
    }

    public void AssignItem(ItemData source, int amount)
    {
        itemData = source;
        quantity = amount;
    }

    public int GetRoomInStack()
    {
        if (itemData == null) return itemData.maxStackSize;
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