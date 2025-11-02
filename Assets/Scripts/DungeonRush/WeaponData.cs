using UnityEngine;

namespace DungeonRush
{
    [System.Serializable]
    public class ComboStep
    {
        public int animationID;
        public float damageMultiplier = 1f;
        [Tooltip("Lực đẩy nhân vật về phía trước khi thực hiện đòn đánh này.")]
        public float lungeForce = 5f;
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "DungeonRush/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Info")]
        public string weaponName;
        [Min(0)] public float baseDamage = 10f;

        [Header("Combat")]
        public ComboStep[] comboSteps;
    }
}