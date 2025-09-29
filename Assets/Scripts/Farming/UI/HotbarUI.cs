using UnityEngine;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private ActiveToolSystem activeToolSystem;
    [SerializeField] private List<HotbarSlotUI> hotbarSlots;

    void Start()
    {
        Redraw();
        UpdateSelection();
    }

    void OnEnable()
    {
        inventorySystem.OnInventoryChanged += Redraw;
        activeToolSystem.OnActiveSlotChanged += UpdateSelection;
    }

    void OnDisable()
    {
        inventorySystem.OnInventoryChanged -= Redraw;
        activeToolSystem.OnActiveSlotChanged -= UpdateSelection;
    }

    private void Redraw()
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].UpdateSlot(inventorySystem.GetSlotAt(i));
        }
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].SetHighlight(i == activeToolSystem.ActiveSlotIndex);
        }
    }
}