using UnityEngine;

namespace DungeonRush.StatusEffects
{
    public abstract class StatusEffectData : ScriptableObject
    {
        public string effectName;
        public Sprite icon;
        public float duration;
        public float tickFrequency = 1f;

        public abstract void OnApply(StatusEffectController target);
        public abstract void OnTick(StatusEffectController target);
        public abstract void OnEnd(StatusEffectController target);
    }
}