using UnityEngine;

namespace MonsterLegion.Data
{
    [CreateAssetMenu(fileName = "NewStatsProfile", menuName = "Monster Legion/Data/Stats Growth Profile")]
    public class StatsProfileSO : ScriptableObject
    {
        [Tooltip("Đường cong tăng trưởng Máu (HP) theo cấp độ (Level 1 to 100).")]
        public AnimationCurve HealthGrowthCurve;

        [Tooltip("Đường cong tăng trưởng Sức Tấn Công (ATK) theo cấp độ.")]
        public AnimationCurve AttackGrowthCurve;

        [Tooltip("Đường cong tăng trưởng Phòng Thủ (DEF) theo cấp độ.")]
        public AnimationCurve DefenseGrowthCurve;

        public BaseStats GetStatsForLevel(int level)
        {
            float normalizedLevel = (float)level / 100.0f;
            return new BaseStats
            {
                Health = Mathf.RoundToInt(HealthGrowthCurve.Evaluate(normalizedLevel)),
                Attack = Mathf.RoundToInt(AttackGrowthCurve.Evaluate(normalizedLevel)),
                Defense = Mathf.RoundToInt(DefenseGrowthCurve.Evaluate(normalizedLevel))
            };
        }
    }

    public struct BaseStats
    {
        public int Health;
        public int Attack;
        public int Defense;
    }
}