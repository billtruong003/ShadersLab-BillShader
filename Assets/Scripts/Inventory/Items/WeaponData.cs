// Path: Assets/Scripts/Inventory/Items/WeaponData.cs
using UnityEngine;

public enum ElementType { None, Fire, Ice, Lightning, Earth, Light, Void }
public enum WeaponType { Sword, Axe, Dagger, Shield, Staff, Book, Bow }

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