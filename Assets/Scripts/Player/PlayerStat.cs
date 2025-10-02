// Path: Assets/Scripts/Player/PlayerStats.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsChanged;
    public event Action<ElementType> OnMilestoneUnlocked;

    private readonly Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
    private readonly Dictionary<StatType, float> additiveModifiers = new Dictionary<StatType, float>();
    private readonly Dictionary<StatType, float> multiplicativeModifiers = new Dictionary<StatType, float>();

    private readonly List<ElementType> elementalTags = new List<ElementType>();

    private void Awake()
    {
        InitializeStats();
    }

    private void InitializeStats()
    {
        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
        {
            baseStats[stat] = 0;
            additiveModifiers[stat] = 0;
            multiplicativeModifiers[stat] = 1;
        }

        // --- Cấu hình chỉ số cơ bản của nhân vật tại đây ---
        baseStats[StatType.MaxHealth] = 100f;
        baseStats[StatType.MoveSpeed] = 5f;
        baseStats[StatType.XPGain] = 1f; // Mặc định là 100%
        baseStats[StatType.Damage] = 1f; // Mặc định là 100%
        baseStats[StatType.Cooldown] = 1f; // Mặc định là 100% (giảm hồi chiêu sẽ trừ đi từ đây)
        baseStats[StatType.AreaSize] = 1f; // Mặc định là 100%
    }

    public float GetStat(StatType stat)
    {
        float baseValue = baseStats.ContainsKey(stat) ? baseStats[stat] : 0f;
        float additive = additiveModifiers.ContainsKey(stat) ? additiveModifiers[stat] : 0f;
        float multiplicative = multiplicativeModifiers.ContainsKey(stat) ? multiplicativeModifiers[stat] : 1f;

        return (baseValue + additive) * multiplicative;
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        foreach (var modifier in upgrade.Modifiers)
        {
            ApplyModifier(modifier);
        }

        if (upgrade.ElementalTag != ElementType.None)
        {
            AddElementalTag(upgrade.ElementalTag);
        }

        OnStatsChanged?.Invoke();
        Debug.Log($"Applied Upgrade: {upgrade.Title}");
    }

    public void ApplyModifier(StatModifier modifier)
    {
        if (modifier.IsPercentage)
        {
            multiplicativeModifiers[modifier.Stat] += modifier.Value / 100f;
        }
        else
        {
            additiveModifiers[modifier.Stat] += modifier.Value;
        }
    }

    private void AddElementalTag(ElementType tag)
    {
        elementalTags.Add(tag);
        CheckForMilestones(tag);
    }

    private void CheckForMilestones(ElementType addedTag)
    {
        int count = elementalTags.Count(t => t == addedTag);
        if (count == 3) // Mở khóa Cột mốc nguyên tố (3 tags)
        {
            Debug.Log($"Milestone Unlocked: {addedTag} Tier 1!");
            OnMilestoneUnlocked?.Invoke(addedTag);
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier.IsPercentage)
        {
            multiplicativeModifiers[modifier.Stat] -= modifier.Value / 100f;
        }
        else
        {
            additiveModifiers[modifier.Stat] -= modifier.Value;
        }
        OnStatsChanged?.Invoke();
    }
}