// Assets/Scripts/TuTien/Core/CharacterStats.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoTanTuTien.Core
{
    [System.Serializable]
    public struct StatModifier
    {
        public float DamageBonus;
        public float DefenseBonus;
    }

    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "VoTanTuTien/Character Stats")]
    public class CharacterStats : ScriptableObject
    {
        public event Action<float, float> OnHealthChanged;
        public event Action<long> OnLinhLucGained;
        public event Action<long> OnLinhNangGained;

        [Header("Primary Progression")]
        public long linhLuc;
        public long linhNang;

        [Header("Core Attributes")]
        public float maxHealth;
        public float maxMana;
        public float baseDamage;
        public float baseDefense;

        [Header("Runtime Values")]
        [NonSerialized] public float currentHealth;
        [NonSerialized] public float currentMana;
        [NonSerialized] private List<StatModifier> activeModifiers = new List<StatModifier>();

        public void InitializeRuntimeValues()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            activeModifiers.Clear();
        }

        public void AddLinhLuc(long amount)
        {
            if (amount <= 0) return;
            linhLuc += amount;
            OnLinhLucGained?.Invoke(amount);
        }

        public void AddLinhNang(long amount)
        {
            if (amount <= 0) return;
            linhNang += amount;
            OnLinhNangGained?.Invoke(amount);
        }

        public bool TrySpendLinhNang(long amount)
        {
            if (linhNang >= amount)
            {
                linhNang -= amount;
                return true;
            }
            return false;
        }

        public void TakeDamage(float amount)
        {
            float effectiveDamage = Mathf.Max(1, amount - GetCurrentDefense());
            currentHealth = Mathf.Max(0, currentHealth - effectiveDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public float GetCurrentDamage()
        {
            float totalBonus = 1f + activeModifiers.Sum(mod => mod.DamageBonus);
            return baseDamage * totalBonus;
        }

        public float GetCurrentDefense()
        {
            float totalBonus = 1f + activeModifiers.Sum(mod => mod.DefenseBonus);
            return baseDefense * totalBonus;
        }

        public void UseMana(float amount)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
        }

        public bool HasEnoughMana(float amount)
        {
            return currentMana >= amount;
        }

        public void AddModifier(StatModifier modifier) => activeModifiers.Add(modifier);
        public void RemoveModifier(StatModifier modifier) => activeModifiers.Remove(modifier);
    }
}