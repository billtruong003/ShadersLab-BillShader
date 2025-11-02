using DungeonRush.Core;
using DungeonRush.StatusEffects;
using UnityEngine;

namespace DungeonRush.Items
{
    [CreateAssetMenu(fileName = "NewConsumableData", menuName = "DungeonRush/Items/Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("Consumable Effects")]
        [SerializeField] private float healthToRestore = 0f;
        [SerializeField] private StatusEffectData statusEffectToApply;

        public override void Use(GameObject user)
        {
            if (healthToRestore > 0)
            {
                var healthComponent = user.GetComponent<HealthComponent>();
                healthComponent?.Heal(healthToRestore);
            }

            if (statusEffectToApply != null)
            {
                var effectController = user.GetComponent<StatusEffectController>();
                effectController?.ApplyEffect(statusEffectToApply);
            }
        }
    }
}