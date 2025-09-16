using UnityEngine;
using System.Collections.Generic;
using MonsterLegion.Data;

namespace MonsterLegion.Gameplay
{
    [CreateAssetMenu(fileName = "Stage_01-01", menuName = "Monster Legion/Gameplay/Stage")]
    public class StageSO : ScriptableObject
    {
        [System.Serializable]
        public struct EnemyPlacement
        {
            public MonsterSpeciesSO Species;
            public int Level;
            public Vector2Int GridPosition;
        }
        public LootTableSO Rewards;
        public List<EnemyPlacement> EnemyFormations;
    }
}