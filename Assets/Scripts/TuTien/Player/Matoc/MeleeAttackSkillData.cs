// Assets/Scripts/TuTien/Skills/MeleeAttackSkillData.cs
using UnityEngine;
using VoTanTuTien.Interfaces;
using VoTanTuTien.Player;
using Sirenix.OdinInspector;
using VoTanTuTien.UI;

namespace VoTanTuTien.Skills
{
    [CreateAssetMenu(menuName = "VoTanTuTien/Skills/Ma Toc/Melee Attack")]
    public class MeleeAttackSkillData : SkillData
    {
        [Title("Melee Specifics")]
        public float damageMultiplier = 1.0f;
        public GameObject hitEffectPrefab;

        public float GetCurrentDamageMultiplier()
        {
            float bonus = 0;
            for (int i = 0; i < currentLevel; i++)
            {
                bonus += upgradeTiers[i].damageMultiplierBonus;
            }
            return damageMultiplier + bonus;
        }

        public override void Activate(MaTocCharacter caster)
        {
            if (caster.CurrentTarget == null || caster.CurrentTarget.IsDead()) return;

            IAttackable target = caster.CurrentTarget;
            float finalDamage = caster.Stats.GetCurrentDamage() * GetCurrentDamageMultiplier();

            // THAY ĐỔI CỐT LÕI NẰM Ở ĐÂY
            target.ReceiveDamage(finalDamage, caster);

            VoTanTuTien.UI.FloatingTextManager.Instance.ShowDamage(finalDamage, target.GetTransform().position + Vector3.up * 2.5f);

            if (hitEffectPrefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(hitEffectPrefab, target.GetTransform().position, Quaternion.identity);
            }
        }
    }
}