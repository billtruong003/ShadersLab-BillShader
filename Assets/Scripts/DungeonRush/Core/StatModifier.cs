using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonRush.Core
{
    public enum StatType
    {
        Health,
        Damage,
        Defense,
        WalkSpeed,
        RunSpeed,
        AttackSpeed,
        RotationSpeed,
        MoveForce,
        DashForce,
        DashDuration,
        DashCooldown
    }

    public enum ModType
    {
        Flat,
        Percent,
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModType type;
        public float value;
        public object source;
    }

    public class Stat
    {
        public float BaseValue { get; private set; }
        public float Value { get; private set; }

        private readonly List<StatModifier> modifiers = new List<StatModifier>();

        public Stat(float baseValue)
        {
            BaseValue = baseValue;
            RecalculateValue();
        }

        public void AddModifier(StatModifier modifier)
        {
            modifiers.Add(modifier);
            RecalculateValue();
        }

        public void RemoveAllModifiersFromSource(object source)
        {
            int numRemoved = modifiers.RemoveAll(mod => mod.source == source);
            if (numRemoved > 0)
            {
                RecalculateValue();
            }
        }

        private void RecalculateValue()
        {
            float finalValue = BaseValue;
            float percentSum = 0;

            foreach (var mod in modifiers)
            {
                if (mod.type == ModType.Flat)
                {
                    finalValue += mod.value;
                }
                else if (mod.type == ModType.Percent)
                {
                    percentSum += mod.value;
                }
            }

            Value = finalValue * (1f + percentSum / 100f);
        }
    }
}