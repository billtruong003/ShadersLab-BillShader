// Path: Assets/Scripts/Inventory/Items/CosmeticItemData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Cosmetic", menuName = "Elemental Echoes/Items/Cosmetic")]
public class CosmeticItemData : ItemData
{
    [Header("Cosmetic Details")]
    public CosmeticSlotType slotType;
    public GameObject cosmeticPrefab; // Prefab hoặc model sẽ được hiển thị trên nhân vật

    public override void Use(GameObject user)
    {
        Debug.Log($"Equipping cosmetic {itemName}.");
        // Tương tự, sẽ được quản lý bởi EquipmentSystem
    }
}