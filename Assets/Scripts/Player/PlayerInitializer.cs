// Path: Assets/Scripts/Debug/PlayerInitializer.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerInitializer : MonoBehaviour
{
    [SerializeField] private List<EquipmentItemData> startingEquipment;
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private EquipmentSystem equipmentSystem;

    void Start()
    {
        InitializeEquipment();
    }

    private void InitializeEquipment()
    {
        foreach (var item in startingEquipment)
        {
            equipmentSystem.Equip(item);
        }
    }
}