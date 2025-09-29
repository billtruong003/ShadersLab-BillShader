using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventorySystem inventorySystem;

    public bool AddItem(ItemData item, int quantity)
    {
        if (inventorySystem == null) return false;
        return inventorySystem.AddItem(item, quantity);
    }

    public bool RemoveItem(ItemData item, int quantity)
    {
        if (inventorySystem == null) return false;
        return inventorySystem.RemoveItem(item, quantity);
    }

    public ItemData GetItemAt(int index)
    {
        return inventorySystem.GetItemAt(index);
    }

    public int GetInventorySize()
    {
        return inventorySystem.GetSize();
    }

    [Header("Testing")]
    [SerializeField] private ItemData testItemToAdd;
    [SerializeField] private int testQuantity = 1;

    [ContextMenu("Test Add Item")]
    private void TestAddItem()
    {
        if (testItemToAdd != null)
        {
            AddItem(testItemToAdd, testQuantity);
        }
    }
}