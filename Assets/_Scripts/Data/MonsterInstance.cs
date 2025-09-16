using System;

namespace MonsterLegion.Data
{
    [Serializable]
    public class MonsterInstance
    {
        public long InstanceID;
        public string SpeciesID;
        public int CurrentLevel;
        public long CurrentXP;
        public int StarTier;
        public int AwakeningTier;
        public Personality Personality;
        public bool IsLocked;
        public bool IsInEliteSquad;

        public MonsterInstance(string speciesID, Personality personality)
        {
            InstanceID = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SpeciesID = speciesID;
            Personality = personality;
            CurrentLevel = 1;
            CurrentXP = 0;
            StarTier = 1;
            AwakeningTier = 0;
            IsLocked = false;
            IsInEliteSquad = false;
        }

        public RuntimeStats CalculateRuntimeStats(MonsterSpeciesSO speciesData)
        {
            if (speciesData == null || speciesData.StatsGrowthProfile == null)
            {
                return new RuntimeStats();
            }

            BaseStats baseStats = speciesData.StatsGrowthProfile.GetStatsForLevel(CurrentLevel);

            float attackModifier = 1.0f;
            float healthModifier = 1.0f;
            float defenseModifier = 1.0f;

            switch (Personality)
            {
                case Personality.Brave: attackModifier = 1.15f; defenseModifier = 0.90f; break;
                case Personality.Reckless: attackModifier = 1.25f; healthModifier = 0.80f; break;
                case Personality.Cautious: healthModifier = 1.25f; attackModifier = 0.85f; break;
                case Personality.Timid: defenseModifier = 1.15f; attackModifier = 0.90f; break;
            }

            return new RuntimeStats
            {
                Element = speciesData.Element,
                MaxHealth = (int)(baseStats.Health * healthModifier),
                Attack = (int)(baseStats.Attack * attackModifier),
                Defense = (int)(baseStats.Defense * defenseModifier)
            };
        }
    }

    public struct RuntimeStats
    {
        public Element Element;
        public int MaxHealth;
        public int Attack;
        public int Defense;
    }
}