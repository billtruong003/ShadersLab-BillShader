using DungeonRush.Inventories;
using DungeonRush.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonRush.UI
{
    public class CharacterPanelUI : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EquipmentManager equipmentManager;
        [SerializeField] private GameObject characterPanelObject;

        [Header("UI Slots")]
        [SerializeField] private Transform slotsContainer;
        private Dictionary<EquipmentSlot, EquipmentSlotUI> slotUIMap;

        private void Awake()
        {
            InitializeSlotMap();
        }

        private void Start()
        {
            if (equipmentManager == null)
            {
                // Tự động tìm kiếm nếu chưa được gán
                equipmentManager = FindFirstObjectByType<EquipmentManager>();
            }

            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
            }

            InitialDraw();
            characterPanelObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            }

            // Hủy đăng ký sự kiện click
            foreach (var slotUI in slotUIMap.Values)
            {
                slotUI.OnUnequipRequested -= HandleUnequipRequest;
            }
        }

        private void Update()
        {
            // Sử dụng phím 'C' (Character) để đóng/mở panel
            if (Input.GetKeyDown(KeyCode.C))
            {
                characterPanelObject.SetActive(!characterPanelObject.activeSelf);
            }
        }

        private void InitializeSlotMap()
        {
            slotUIMap = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
            EquipmentSlotUI[] slots = slotsContainer.GetComponentsInChildren<EquipmentSlotUI>();

            foreach (var slotUI in slots)
            {
                if (!slotUIMap.ContainsKey(slotUI.EquipmentSlotType))
                {
                    slotUIMap[slotUI.EquipmentSlotType] = slotUI;
                    slotUI.OnUnequipRequested += HandleUnequipRequest; // Đăng ký sự kiện
                }
            }
        }

        private void InitialDraw()
        {
            var equippedItems = equipmentManager.GetEquippedItems();
            foreach (var slotEnum in slotUIMap.Keys)
            {
                equippedItems.TryGetValue(slotEnum, out EquipmentData item);
                slotUIMap[slotEnum].UpdateSlot(item);
            }
        }

        private void HandleEquipmentChanged(EquipmentSlot slot, EquipmentData oldItem, EquipmentData newItem)
        {
            if (slotUIMap.TryGetValue(slot, out EquipmentSlotUI slotUI))
            {
                slotUI.UpdateSlot(newItem);
            }
        }

        private void HandleUnequipRequest(EquipmentSlot slotToUnequip)
        {
            equipmentManager.Unequip(slotToUnequip);
        }
    }
}