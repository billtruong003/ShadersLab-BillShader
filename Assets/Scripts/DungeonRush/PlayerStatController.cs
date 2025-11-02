using DungeonRush.Core;
using DungeonRush.Inventories;
using DungeonRush.Items;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonRush.Stats
{
    public class PlayerStatController : MonoBehaviour
    {
        [InlineEditor]
        [SerializeField] private PlayerStats baseStats;
        private EquipmentManager equipmentManager;

        private readonly Dictionary<DungeonRush.Core.StatType, Stat> stats = new Dictionary<Core.StatType, Stat>();

        public event Action OnStatsChanged;

        private void Awake()
        {
            InitializeBaseStats();

            equipmentManager = GetComponent<EquipmentManager>();
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += HandleEquipmentChange;
            }
        }

        private void OnDestroy()
        {
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= HandleEquipmentChange;
            }
        }

        public float GetStat(Core.StatType type)
        {
            return stats.TryGetValue(type, out Stat stat) ? stat.Value : 0f;
        }

        private void InitializeBaseStats()
        {
            // Movement
            stats[Core.StatType.WalkSpeed] = new Stat(baseStats.walkSpeed);
            stats[Core.StatType.RunSpeed] = new Stat(baseStats.runSpeed);
            stats[Core.StatType.MoveForce] = new Stat(baseStats.moveForce);
            stats[Core.StatType.RotationSpeed] = new Stat(baseStats.rotationSpeed);

            // Dashing
            stats[Core.StatType.DashForce] = new Stat(baseStats.dashForce);
            stats[Core.StatType.DashDuration] = new Stat(baseStats.dashDuration);
            stats[Core.StatType.DashCooldown] = new Stat(baseStats.dashCooldown);

            // Combat
            stats[Core.StatType.Damage] = new Stat(10f); // Giá trị cơ bản
            stats[Core.StatType.Defense] = new Stat(5f);  // Giá trị cơ bản
        }

        private void HandleEquipmentChange(EquipmentSlot slot, EquipmentData oldItem, EquipmentData newItem)
        {
            if (oldItem != null)
            {
                RemoveModifiersFromSource(oldItem);
            }
            if (newItem != null)
            {
                AddModifiersFromSource(newItem);
            }
            OnStatsChanged?.Invoke();
        }

        private void AddModifiersFromSource(EquipmentData source)
        {
            foreach (var modifier in source.statModifiers)
            {
                if (stats.TryGetValue(modifier.stat, out Stat stat))
                {
                    var modWithSource = modifier;
                    modWithSource.source = source;
                    stat.AddModifier(modWithSource);
                }
            }
        }

        private void RemoveModifiersFromSource(EquipmentData source)
        {
            foreach (var modifier in source.statModifiers)
            {
                if (stats.TryGetValue(modifier.stat, out Stat stat))
                {
                    stat.RemoveAllModifiersFromSource(source);
                }
            }
        }
    }
}