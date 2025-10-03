using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private ItemDetailPopup itemDetailPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (itemDetailPopup != null)
        {
            itemDetailPopup.gameObject.SetActive(false);
        }
    }

    public void ShowPopup(InventorySlot slot, int sourceSlotIndex)
    {
        if (slot == null || slot.itemData == null) return;
        itemDetailPopup.Display(slot, sourceSlotIndex);
    }

    public void HidePopup()
    {
        itemDetailPopup.Hide();
    }

    public void RequestUseItem(ItemData item, int sourceSlotIndex)
    {
        var inventorySystem = GameDataManager.Instance.InventorySystem;
        var equipmentSystem = GameDataManager.Instance.EquipmentSystem;
        var playerObject = GameDataManager.Instance.GetCurrentPlayerStats().gameObject;

        if (item is EquipmentItemData equipmentItem)
        {
            bool equipped = equipmentSystem.Equip(equipmentItem);
            if (equipped)
            {
                inventorySystem.RemoveItemAt(sourceSlotIndex, 1);
            }
        }
        else
        {
            item.Use(playerObject);
            if (item is ConsumableItemData)
            {
                inventorySystem.RemoveItem(item, 1);
            }
        }
        HidePopup();
    }

    public void RequestDropItem(int sourceSlotIndex, bool dropAll)
    {
        var inventorySystem = GameDataManager.Instance.InventorySystem;
        var slot = inventorySystem.GetSlotAt(sourceSlotIndex);
        if (slot == null || slot.itemData == null) return;

        int quantityToDrop = dropAll ? slot.quantity : 1;

        inventorySystem.RemoveItemAt(sourceSlotIndex, quantityToDrop);

        // TODO: Logic để vứt item ra thế giới (Instantiate một prefab pickup)

        HidePopup();
    }
}