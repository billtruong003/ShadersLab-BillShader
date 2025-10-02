// Path: Assets/Scripts/UI/EquipmentPanelUI.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EquipmentPanelUI : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private EquipmentSystem equipmentSystem;

    private List<EquipmentSlotUI> equipmentSlots;

    private void Start()
    {
        equipmentSlots = slotsContainer.GetComponentsInChildren<EquipmentSlotUI>().ToList();
        foreach (var slot in equipmentSlots)
        {
            slot.Initialize(equipmentSystem);
        }
        equipmentSystem.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDestroy()
    {
        if (equipmentSystem != null)
        {
            equipmentSystem.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    private void HandleEquipmentChanged(EquipmentSlotType changedSlotType)
    {
        var slotToUpdate = equipmentSlots.FirstOrDefault(s => s.slotType == changedSlotType);
        if (slotToUpdate != null)
        {
            slotToUpdate.UpdateSlotVisual(equipmentSystem.GetEquippedItem(changedSlotType));
        }
    }
}