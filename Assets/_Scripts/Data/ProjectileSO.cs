using UnityEngine;

namespace MonsterLegion.Data
{
    [CreateAssetMenu(fileName = "NewProjectile", menuName = "Monster Legion/Data/Projectile")]
    public class ProjectileSO : ScriptableObject
    {
        [Header("Identification")]
        public string ProjectileID;

        [Header("Core Gameplay")]
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 20f;
        public float BaseDamage = 10f;

        [Header("Movement & Impact")]
        public ProjectileMovementType MovementType;
        public ProjectileImpactEffectType ImpactEffectType;

        [Header("Impact Parameters")]
        [Tooltip("Bán kính ảnh hưởng cho loại đạn AreaOfEffect.")]
        public float AoeRadius = 3f;

        [Tooltip("Số lượng mục tiêu có thể xuyên qua cho loại đạn Pierce.")]
        public int PierceCount = 1;

        [Tooltip("Số lần nảy sang mục tiêu khác cho loại đạn Chain.")]
        public int ChainCount = 2;

        [Tooltip("Bán kính tìm mục tiêu để nảy sang cho loại đạn Chain.")]
        public float ChainRadius = 5f;
    }
}