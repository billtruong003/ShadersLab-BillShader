using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private InventorySystem inventorySystem => GameDataManager.Instance.InventorySystem;

    public bool AddItem(ItemData item, int quantity)
    {
        return inventorySystem?.AddItem(item, quantity) ?? false;
    }

    public bool RemoveItem(ItemData item, int quantity)
    {
        return inventorySystem?.RemoveItem(item, quantity) ?? false;
    }

    public bool RemoveItemAt(int index, int quantity)
    {
        return inventorySystem?.RemoveItemAt(index, quantity) ?? false;
    }

    public ItemData GetItemAt(int index)
    {
        return inventorySystem?.GetItemAt(index);
    }

    public int GetInventorySize()
    {
        return inventorySystem?.GetSize() ?? 0;
    }
}
