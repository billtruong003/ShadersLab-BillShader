using UnityEngine;
using MonsterLegion.Data;

namespace MonsterLegion.Combat
{
    public static class DamageCalculator
    {
        private const int MINIMUM_DAMAGE = 1;

        public static int Calculate(RuntimeStats attackerStats, RuntimeStats targetStats, ProjectileSO projectile)
        {
            float basePower = attackerStats.Attack + projectile.BaseDamage;
            float rawDamage = basePower - targetStats.Defense;

            float elementalMultiplier = ElementalAdvantageSystem.GetDamageMultiplier(attackerStats.Element, targetStats.Element);
            int finalDamage = Mathf.RoundToInt(rawDamage * elementalMultiplier);

            return Mathf.Max(MINIMUM_DAMAGE, finalDamage);
        }
    }
}