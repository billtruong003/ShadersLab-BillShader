using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterLegion.Data
{
    [Serializable]
    public class PlayerData
    {
        [Header("Commander Info")]
        public string PlayerName;
        public int CommanderLevel;
        public long CommanderXP;

        [Header("Currencies")]
        public long Gold;
        public int Gems;
        public long Food;
        public int Essence;

        [Header("Gameplay Resources")]
        public int CurrentEnergy;
        public long LastEnergyRegenTimestamp; // Sử dụng Unix timestamp

        [Header("Owned Assets")]
        public List<MonsterInstance> OwnedMonsters;
        public List<StructureInstance> OwnedStructures;
        public List<HatcherySlot> HatcherySlots;


        // Sẽ thêm các hệ thống khác như QuestProgress, Achievements sau...

        public PlayerData(string playerName)
        {
            PlayerName = playerName;

            // Giá trị khởi đầu cho người chơi mới
            CommanderLevel = 1;
            CommanderXP = 0;

            Gold = 500;
            Gems = 20;
            Food = 200;
            Essence = 0;

            CurrentEnergy = 50; // Giả sử giới hạn ban đầu là 50
            LastEnergyRegenTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            OwnedMonsters = new List<MonsterInstance>();
            OwnedStructures = new List<StructureInstance>();

            // Khởi tạo game với một số quái vật và công trình cơ bản theo GDD
            InitializeFirstTimeSetup();
        }

        private void InitializeFirstTimeSetup()
        {
            // Logic này có thể được mở rộng để tạo ra các quái vật, công trình
            // đầu tiên cho người chơi theo kịch bản tutorial.
            // Ví dụ:
            // MonsterInstance startingMonster = new MonsterInstance("stone_golem_01", Personality.Neutral);
            // OwnedMonsters.Add(startingMonster);
            //
            // StructureInstance startingHabitat = new StructureInstance("earth_habitat_01", new Vector2Int(0,0));
            // OwnedStructures.Add(startingHabitat);
        }
    }
}

[System.Serializable]
public class HatcherySlot
{
    public bool IsHatching;
    public string EggID;
    public long HatchEndTime; // Unix Timestamp
}
