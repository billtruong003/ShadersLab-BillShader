// Assets/Scripts/TuTien/Skills/SkillData.cs
using UnityEngine;
using VoTanTuTien.Player;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace VoTanTuTien.Skills
{
    public enum SkillType { Melee, Ranged, SelfBuff }

    [Serializable]
    public class SkillUpgradeTier
    {
        public long linhNangCost = 10;
        [Header("Bonus Stats")]
        public float damageMultiplierBonus = 0.1f;
        public float cooldownReduction = 0.2f;

    }

    public abstract class SkillData : ScriptableObject
    {
        [Title("Thông tin Cơ bản")]
        [PreviewField(75)] public Sprite icon;
        public string skillName;
        [TextArea] public string description;

        [Title("Thuộc tính Chiến đấu")]
        public SkillType type;
        public float manaCost;
        public float cooldown;
        public string animationTrigger;

        [ShowIf("type", SkillType.Melee)]
        [BoxGroup("Phạm vi")]
        [SuffixLabel("m")] public float attackRange = 2.5f;

        [ShowIf("type", SkillType.Ranged)]
        [BoxGroup("Phạm vi")]
        [SuffixLabel("m")] public float optimalRange = 15f;

        [ShowIf("type", SkillType.Ranged)]
        [BoxGroup("Phạm vi")]
        [Tooltip("Nếu mục tiêu ở gần hơn khoảng cách này, nhân vật sẽ dịch chuyển ra xa trước khi tấn công.")]
        [SuffixLabel("m")] public float minTeleportRange = 5f;

        [Title("Hệ Thống Nâng Cấp")]
        [NonSerialized] public int currentLevel = 0;
        public List<SkillUpgradeTier> upgradeTiers;

        public int MaxLevel => upgradeTiers.Count;
        public bool IsMaxLevel() => currentLevel >= MaxLevel;
        public SkillUpgradeTier GetNextUpgradeTier()
        {
            if (IsMaxLevel()) return null;
            return upgradeTiers[currentLevel];
        }

        public virtual float GetCurrentCooldown()
        {
            float reduction = 0;
            for (int i = 0; i < currentLevel; i++)
            {
                reduction += upgradeTiers[i].cooldownReduction;
            }
            return Mathf.Max(0.5f, cooldown - reduction); // Đảm bảo cooldown không bao giờ quá thấp
        }

        public abstract void Activate(MaTocCharacter caster);
    }
}