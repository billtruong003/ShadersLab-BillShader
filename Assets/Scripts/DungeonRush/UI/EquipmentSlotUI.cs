using DungeonRush.Items;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonRush.UI
{
    [RequireComponent(typeof(Button))]
    public class EquipmentSlotUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite defaultIcon;

        [Header("Slot Configuration")]
        [SerializeField] private EquipmentSlot equipmentSlotType;

        private Button button;

        public event Action<EquipmentSlot> OnUnequipRequested;
        public EquipmentSlot EquipmentSlotType => equipmentSlotType;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
            UpdateSlot(null); // Bắt đầu với trạng thái trống
        }

        public void UpdateSlot(EquipmentData equippedItem)
        {
            bool hasItem = equippedItem != null;

            iconImage.sprite = hasItem ? equippedItem.icon : defaultIcon;
            iconImage.color = hasItem ? Color.white : new Color(1, 1, 1, 0.5f); // Làm mờ icon mặc định
        }

        private void HandleClick()
        {
            OnUnequipRequested?.Invoke(equipmentSlotType);
        }
    }
}