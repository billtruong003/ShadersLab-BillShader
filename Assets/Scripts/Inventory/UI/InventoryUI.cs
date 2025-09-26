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
        InitializeUI();
        inventorySystem.OnInventoryChanged += Redraw;
        Redraw();
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
            slotUIs.Add(slotInstance.GetComponent<InventorySlotUI>());
        }
    }

    private void Redraw()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inventorySystem.InventorySlots.Count)
            {
                slotUIs[i].UpdateSlot(inventorySystem.InventorySlots[i]);
            }
            else
            {
                slotUIs[i].ClearSlot();
            }
        }
    }
}