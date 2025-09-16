using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MonsterLegion.Data;

namespace MonsterLegion.Gameplay
{
    [System.Serializable]
    public struct LootDrop
    {
        public MonsterSpeciesSO MonsterToDrop;
        public EggSO EggToDrop;
        [Range(0f, 100f)]
        public float DropChancePercentage;
    }

    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Monster Legion/Gameplay/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public List<LootDrop> PossibleDrops;

        public MonsterSpeciesSO PickOneMonster()
        {
            float totalWeight = PossibleDrops.Sum(drop => drop.DropChancePercentage);
            float randomValue = Random.Range(0, totalWeight);
            float cumulativeWeight = 0;

            foreach (var drop in PossibleDrops)
            {
                cumulativeWeight += drop.DropChancePercentage;
                if (randomValue <= cumulativeWeight)
                {
                    return drop.MonsterToDrop;
                }
            }
            return null;
        }
    }
}