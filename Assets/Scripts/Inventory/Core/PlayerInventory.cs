using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventorySystem inventorySystem;

    // Public API để các hệ thống khác (như QuestSystem) gọi vào
    public bool AddQuestItem(ItemData item, int quantity)
    {
        if (inventorySystem == null)
        {
            Debug.LogError("Inventory System is not assigned!");
            return false;
        }

        bool success = inventorySystem.AddItem(item, quantity);

        if (success)
        {
            Debug.Log($"Successfully added {quantity} of {item.itemName} to inventory.");
        }
        else
        {
            Debug.LogWarning($"Failed to add {item.itemName} to inventory. It might be full.");
        }

        return success;
    }

    // Ví dụ sử dụng để test
    [Header("Testing")]
    [SerializeField] private ItemData testItemToAdd;
    [SerializeField] private int testQuantity = 1;

    [ContextMenu("Test Add Item")]
    private void TestAddItem()
    {
        if (testItemToAdd != null)
        {
            AddQuestItem(testItemToAdd, testQuantity);
        }
    }
}