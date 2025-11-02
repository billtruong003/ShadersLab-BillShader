using DungeonRush.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonRush.StatusEffects
{
    public class ActiveEffect
    {
        public StatusEffectData Data { get; }
        public float RemainingTime { get; set; }
        private float tickTimer;

        public ActiveEffect(StatusEffectData data)
        {
            Data = data;
            RemainingTime = data.duration;
            tickTimer = 0f; // Tick ngay ở frame đầu tiên
        }

        public bool Tick(StatusEffectController target, float deltaTime)
        {
            RemainingTime -= deltaTime;
            tickTimer -= deltaTime;

            if (tickTimer <= 0f)
            {
                Data.OnTick(target);
                tickTimer += Data.tickFrequency;
            }

            return RemainingTime > 0f;
        }
    }

    public class StatusEffectController : MonoBehaviour
    {
        private readonly Dictionary<StatusEffectData, ActiveEffect> activeEffects = new Dictionary<StatusEffectData, ActiveEffect>();
        private readonly List<StatusEffectData> effectsToRemove = new List<StatusEffectData>();

        private void Update()
        {
            if (activeEffects.Count == 0) return;

            foreach (var effect in activeEffects.Values)
            {
                bool isStillActive = effect.Tick(this, Time.deltaTime);
                if (!isStillActive)
                {
                    effectsToRemove.Add(effect.Data);
                }
            }

            if (effectsToRemove.Count > 0)
            {
                foreach (var effectData in effectsToRemove)
                {
                    RemoveEffect(effectData);
                }
                effectsToRemove.Clear();
            }
        }

        public void ApplyEffect(StatusEffectData effectData)
        {
            if (activeEffects.TryGetValue(effectData, out ActiveEffect existingEffect))
            {
                existingEffect.RemainingTime = effectData.duration;
            }
            else
            {
                var newEffect = new ActiveEffect(effectData);
                activeEffects[effectData] = newEffect;
                newEffect.Data.OnApply(this);
            }
        }

        private void RemoveEffect(StatusEffectData effectData)
        {
            if (activeEffects.TryGetValue(effectData, out ActiveEffect effectToEnd))
            {
                effectToEnd.Data.OnEnd(this);
                activeEffects.Remove(effectData);
            }
        }
    }
}