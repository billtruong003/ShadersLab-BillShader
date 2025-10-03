using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EquipmentPanelUI : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;

    private List<EquipmentSlotUI> equipmentSlots;
    private EquipmentSystem equipmentSystem;

    private void Start()
    {
        equipmentSystem = GameDataManager.Instance.EquipmentSystem;

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