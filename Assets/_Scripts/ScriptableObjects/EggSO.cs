using UnityEngine;
using MonsterLegion.Data;

namespace MonsterLegion.Gameplay
{
    [CreateAssetMenu(fileName = "NewEgg", menuName = "Monster Legion/Gameplay/Egg")]
    public class EggSO : ScriptableObject
    {
        public string EggID;
        public string DisplayName;
        public Sprite Icon;
        [Tooltip("Hatching time in seconds.")]
        public int HatchTimeInSeconds;
        [Tooltip("The loot table that defines the monster drop rates.")]
        public LootTableSO MonsterLootTable;
    }
}