using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSystem
{
    public event Action<EquipmentSlotType> OnEquipmentChanged;
    public event Action<CosmeticSlotType> OnCosmeticChanged;

    private readonly Dictionary<EquipmentSlotType, EquipmentItemData> equippedItems = new Dictionary<EquipmentSlotType, EquipmentItemData>();
    private readonly Dictionary<CosmeticSlotType, CosmeticItemData> equippedCosmetics = new Dictionary<CosmeticSlotType, CosmeticItemData>();

    private readonly PlayerStats playerStats;
    private readonly InventorySystem inventorySystem;

    public EquipmentSystem(InventorySystem invSystem, PlayerStats pStats)
    {
        inventorySystem = invSystem;
        playerStats = pStats;
    }

    public EquipmentItemData GetEquippedItem(EquipmentSlotType slotType)
    {
        equippedItems.TryGetValue(slotType, out var item);
        return item;
    }

    public bool Equip(EquipmentItemData itemToEquip)
    {
        if (itemToEquip == null) return false;

        if (equippedItems.TryGetValue(itemToEquip.slotType, out var currentItem))
        {
            Unequip(itemToEquip.slotType);
        }

        equippedItems[itemToEquip.slotType] = itemToEquip;
        ApplyItemModifiers(itemToEquip);
        OnEquipmentChanged?.Invoke(itemToEquip.slotType);
        return true;
    }

    public bool Unequip(EquipmentSlotType slotType)
    {
        if (!equippedItems.TryGetValue(slotType, out var itemToUnequip)) return false;

        if (!inventorySystem.AddItem(itemToUnequip, 1))
        {
            return false;
        }

        RemoveItemModifiers(itemToUnequip);
        equippedItems.Remove(slotType);
        OnEquipmentChanged?.Invoke(slotType);
        return true;
    }

    private void ApplyItemModifiers(EquipmentItemData item)
    {
        foreach (var modifier in item.modifiers)
        {
            playerStats.ApplyModifier(modifier);
        }
    }

    private void RemoveItemModifiers(EquipmentItemData item)
    {
        foreach (var modifier in item.modifiers)
        {
            playerStats.RemoveModifier(modifier);
        }
    }

    // Cosmetic logic remains the same conceptually
    public bool EquipCosmetic(CosmeticItemData cosmetic)
    {
        if (cosmetic == null) return false;

        if (equippedCosmetics.TryGetValue(cosmetic.slotType, out var currentCosmetic))
        {
            UnequipCosmetic(cosmetic.slotType);
        }

        equippedCosmetics[cosmetic.slotType] = cosmetic;
        OnCosmeticChanged?.Invoke(cosmetic.slotType);
        return true;
    }

    public bool UnequipCosmetic(CosmeticSlotType slotType)
    {
        if (!equippedCosmetics.TryGetValue(slotType, out var cosmeticToUnequip)) return false;

        if (!inventorySystem.AddItem(cosmeticToUnequip, 1)) return false;

        equippedCosmetics.Remove(slotType);
        OnCosmeticChanged?.Invoke(slotType);
        return true;
    }
}