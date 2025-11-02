using DungeonRush.Inventories;
using DungeonRush.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonRush.UI
{
    public class ItemDetailPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private Image itemIcon;

        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;

        private DungeonRush.Inventories.InventorySystem inventory;
        private int currentSlotIndex;

        public void Initialize(DungeonRush.Inventories.InventorySystem targetInventory)
        {
            inventory = targetInventory;
            useButton.onClick.AddListener(OnUseButtonClicked);
            dropButton.onClick.AddListener(OnDropButtonClicked);
            Hide();
        }

        public void Show(DungeonRush.Inventories.InventorySlot slot, int slotIndex)
        {
            if (slot == null || slot.Item == null)
            {
                Hide();
                return;
            }

            currentSlotIndex = slotIndex;
            DungeonRush.Items.ItemData item = slot.Item;

            itemNameText.text = item.displayName;
            itemDescriptionText.text = item.description;
            itemIcon.sprite = item.icon;

            useButton.GetComponentInChildren<TextMeshProUGUI>().text = GetUseButtonText(item);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private string GetUseButtonText(DungeonRush.Items.ItemData item)
        {
            if (item is EquipmentData) return "Equip";
            if (item is ConsumableData) return "Use";
            return "Use";
        }

        private void OnUseButtonClicked()
        {
            var itemToUse = inventory.Slots[currentSlotIndex].Item;

            if (itemToUse is EquipmentData equipment)
            {
                equipment.Equip(inventory.gameObject, currentSlotIndex);
            }
            else
            {
                inventory.UseItem(currentSlotIndex);
            }

            Hide();
        }

        private void OnDropButtonClicked()
        {
            inventory.RemoveFromSlot(currentSlotIndex, inventory.Slots[currentSlotIndex].Quantity);
            Hide();
        }
    }
}