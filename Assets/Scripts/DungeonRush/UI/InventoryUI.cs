using DungeonRush.Inventories;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonRush.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private DungeonRush.Inventories.InventorySystem targetInventory;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private ItemDetailPanel detailPanel;

        private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

        private void Start()
        {
            Initialize();
            targetInventory.OnInventoryUpdated += Redraw;
            inventoryPanel.SetActive(false); // Bắt đầu với inventory đóng
            Redraw();
        }

        private void OnDestroy()
        {
            if (targetInventory != null)
            {
                targetInventory.OnInventoryUpdated -= Redraw;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                if (!inventoryPanel.activeSelf)
                {
                    detailPanel.Hide();
                }
            }
        }

        private void Initialize()
        {
            detailPanel.Initialize(targetInventory);
            for (int i = 0; i < targetInventory.Slots.Count; i++)
            {
                InventorySlotUI newSlot = Instantiate(slotPrefab, slotsContainer);
                newSlot.OnSlotClicked += HandleSlotClick;
                slotUIs.Add(newSlot);
            }
        }

        private void Redraw()
        {
            for (int i = 0; i < targetInventory.Slots.Count; i++)
            {
                slotUIs[i].UpdateSlot(targetInventory.Slots[i], i);
            }
        }

        private void HandleSlotClick(int slotIndex)
        {
            var slot = targetInventory.Slots[slotIndex];
            if (slot != null)
            {
                detailPanel.Show(slot, slotIndex);
            }
        }
    }
}