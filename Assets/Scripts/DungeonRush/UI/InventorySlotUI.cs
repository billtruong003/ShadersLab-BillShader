using DungeonRush.Inventories;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonRush.UI
{
    [RequireComponent(typeof(Button))]
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;

        private Button button;
        private int slotIndex;

        public event Action<int> OnSlotClicked;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        public void UpdateSlot(DungeonRush.Inventories.InventorySlot slot, int index)
        {
            slotIndex = index;
            bool hasItem = slot != null && slot.Item != null;

            iconImage.enabled = hasItem;
            quantityText.enabled = hasItem && slot.Quantity > 1;

            if (hasItem)
            {
                iconImage.sprite = slot.Item.icon;
                quantityText.text = slot.Quantity.ToString();
            }
        }

        private void HandleClick()
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
    }
}