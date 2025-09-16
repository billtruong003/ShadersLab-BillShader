using UnityEngine;
using System;
using MonsterLegion.Core;
using MonsterLegion.Data;

namespace MonsterLegion.Gameplay
{
    public class HatcheryManager : MonoBehaviour
    {
        public static HatcheryManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public MonsterInstance CollectHatchedMonster(int slotIndex)
        {
            var slot = GameDataManager.Instance.Data.HatcherySlots[slotIndex];
            if (!slot.IsHatching || System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() < slot.HatchEndTime)
            {
                Debug.LogWarning("Trứng chưa sẵn sàng!");
                return null;
            }

            // Bước 1: Lấy dữ liệu Egg từ Database
            EggSO eggData = GameDatabase.Instance.GetEggByID(slot.EggID);
            if (eggData == null)
            {
                Debug.LogError($"Không tìm thấy Egg với ID: {slot.EggID} trong database!");
                return null;
            }

            // Bước 2: Dùng Loot Table của trứng để chọn ra một loài quái vật
            MonsterSpeciesSO hatchedSpecies = eggData.MonsterLootTable.PickOneMonster();
            if (hatchedSpecies == null)
            {
                Debug.LogError($"Bảng loot của trứng {eggData.DisplayName} có vấn đề!");
                return null;
            }

            // Bước 3: Tạo một MonsterInstance mới
            var personality = (Personality)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(Personality)).Length);
            var newMonster = new MonsterInstance(hatchedSpecies.SpeciesID, personality);

            GameDataManager.Instance.Data.OwnedMonsters.Add(newMonster);

            // Bước 4: Reset slot và lưu game
            slot.IsHatching = false;
            slot.EggID = null;

            GameDataManager.Instance.SaveGame();
            Debug.Log($"Đã nở ra {hatchedSpecies.DisplayName} với tính cách {personality}!");
            return newMonster;
        }
    }
}