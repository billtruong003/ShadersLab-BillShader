using DungeonRush.Core;
using UnityEngine;

namespace DungeonRush.StatusEffects
{
    [CreateAssetMenu(fileName = "NewDoTEffect", menuName = "DungeonRush/Status Effects/Damage Over Time")]
    public class DamageOverTimeEffect : StatusEffectData
    {
        [SerializeField] private float damagePerTick = 5f;

        public override void OnApply(StatusEffectController target) { }

        public override void OnEnd(StatusEffectController target) { }

        public override void OnTick(StatusEffectController target)
        {
            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(damagePerTick);
            }
        }
    }
}