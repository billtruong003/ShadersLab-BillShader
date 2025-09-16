using UnityEngine;

namespace MonsterLegion.Data
{
    [CreateAssetMenu(fileName = "NewMonsterSpecies", menuName = "Monster Legion/Data/Monster Species")]
    public class MonsterSpeciesSO : ScriptableObject
    {
        [Header("Identification & Display")]
        [Tooltip("ID định danh duy nhất, ví dụ: 'fire_golem_01'.")]
        public string SpeciesID;
        public string DisplayName;
        public Sprite Icon;
        public GameObject ModelPrefab;

        [Header("Classification")]
        public Rarity Rarity;
        public Element Element;
        public MonsterClass Class;

        [Header("Growth Profile")]
        [Tooltip("Tham chiếu đến ScriptableObject định nghĩa đường cong tăng trưởng chỉ số.")]
        public StatsProfileSO StatsGrowthProfile;

        [Header("Combat Data")]
        public float AttackRange = 10f;
        public float AttackSpeed = 1.2f;
        public float AttackWindUpTime = 0.2f;

        [Tooltip("Tên của transform con trong prefab model, nơi đạn sẽ được sinh ra.")]
        public string ProjectileMuzzlePointTransformName = "Muzzle_Point";
        public ProjectileSO ProjectileReference;
        public TargetingPriority TargetingLogic;
    }
}