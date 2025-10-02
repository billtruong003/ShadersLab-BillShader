// Path: Assets/Scripts/Inventory/Items/WeaponData.cs
using UnityEngine;

public enum ElementType { None, Fire, Ice, Lightning, Earth, Light, Void }
public enum StatType
{
    // Core Stats
    MaxHealth,
    Armor,
    MoveSpeed,
    XPGain,
    PickupRadius,

    // Offensive Stats
    Damage,
    Cooldown,
    AreaSize,
    ProjectileSpeed,
    Duration,
    ProjectileCount
}
public enum WeaponType { Sword, Axe, Dagger, Aegis, Staff, Book, Orb, Arrow }
public enum EquipmentSlotType
{
    // Armor & Accessories
    Headgear,
    Top,
    Bottom,
    Shoes,
    Gloves,

    // Jewelry & Misc
    Bracelet,
    Earring,
    Watch,
    Mask,
    HandAcc, // Phụ kiện tay
    HairAcc, // Phụ kiện tóc

    // Weapons 
    Weapon1,
    Weapon2
}

// Path: Assets/Scripts/Inventory/Core/CosmeticSlotType.cs
public enum CosmeticSlotType
{
    Hair,
    Eye,
    Eyebrow,
    Eyewear, // Kính mắt
    Lips
}
[CreateAssetMenu(fileName = "WPN_NewWeapon", menuName = "Elemental Echoes/Items/Weapon")]
public class WeaponData : EquipmentItemData
{
    [Header("Weapon Core Stats")]
    public WeaponType type;
    public ElementType element = ElementType.None;
    public GameObject weaponPrefab; // Prefab của vũ khí sẽ được kích hoạt
    public float baseDamage = 10f;
    public float cooldown = 1.5f;
    public float areaOfEffect = 1f; // Bán kính hoặc kích thước
}