// Path: Assets/Scripts/Upgrades/UpgradeData.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StatModifier
{
    public StatType Stat;
    public float Value;
    public bool IsPercentage; // True nếu giá trị là %, False nếu là giá trị cộng thẳng
}

[CreateAssetMenu(fileName = "Upgrade_", menuName = "Elemental Echoes/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("UI Information")]
    public string Title;
    [TextArea(3, 5)]
    public string Description;
    public Sprite Icon;

    [Header("Upgrade Logic")]
    public ElementType ElementalTag = ElementType.None;
    public List<StatModifier> Modifiers;

    // Thêm các trường khác để mở rộng sau này, ví dụ:
    // public WeaponData NewWeaponToUnlock;
    // public WeaponData EvolveFromWeapon;
    // public WeaponData EvolveToWeapon;
}