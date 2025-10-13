// Assets/Scripts/TuTien/Skills/SelfBuffSkillData.cs
using UnityEngine;
using VoTanTuTien.Core;
using VoTanTuTien.Player;
using Sirenix.OdinInspector;

namespace VoTanTuTien.Skills
{
    [CreateAssetMenu(menuName = "VoTanTuTien/Skills/Ma Toc/Self Buff")]
    public class SelfBuffSkillData : SkillData
    {
        [Title("Buff Specifics")]
        public VoTanTuTien.Core.StatModifier modifier;
        public float duration;
        public GameObject buffEffectPrefab;

        public override void Activate(MaTocCharacter caster)
        {
            caster.ApplyBuff(modifier, duration);
            if (buffEffectPrefab != null && ObjectPoolManager.Instance != null)
            {
                GameObject vfx = ObjectPoolManager.Instance.Spawn(buffEffectPrefab, caster.transform.position, Quaternion.identity);
                vfx.transform.SetParent(caster.transform);
            }
        }
    }
}