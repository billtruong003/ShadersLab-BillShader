// Path: Assets/Scripts/Inventory/UI/InventoryUI.cs
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private Transform slotContainer;

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private void Start()
    {
        // Đảm bảo inventorySystem không null
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<PlayerInventory>().GetComponent<InventorySystem>();
        }

        InitializeUI();
        inventorySystem.OnInventoryChanged += Redraw;
    }

    private void OnDestroy()
    {
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged -= Redraw;
        }
    }

    private void InitializeUI()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < inventorySystem.InventorySlots.Count; i++)
        {
            GameObject slotInstance = Instantiate(inventorySlotPrefab, slotContainer);
            var slotUIComponent = slotInstance.GetComponent<InventorySlotUI>();
            slotUIComponent.Initialize(inventorySystem, i); // <-- Dòng quan trọng
            slotUIs.Add(slotUIComponent);
        }
    }

    private void Redraw()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inventorySystem.InventorySlots.Count)
            {
                // UpdateSlot giờ đã được gọi bên trong Initialize và khi event được kích hoạt
                slotUIs[i].UpdateSlot(inventorySystem.GetSlotAt(i));
            }
            else
            {
                slotUIs[i].ClearSlot();
            }
        }
    }
}