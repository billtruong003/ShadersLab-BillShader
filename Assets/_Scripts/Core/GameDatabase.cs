using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MonsterLegion.Data;
using MonsterLegion.Gameplay;

namespace MonsterLegion.Core
{
    public class GameDatabase : MonoBehaviour
    {
        public static GameDatabase Instance { get; private set; }

        private Dictionary<string, MonsterSpeciesSO> monsterSpeciesDatabase;
        private Dictionary<string, EggSO> eggDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDatabases();
        }

        private void LoadDatabases()
        {
            // Tải tất cả MonsterSpeciesSO từ thư mục Resources
            var monsterSpeciesArray = Resources.LoadAll<MonsterSpeciesSO>("ScriptableObjects/MonsterSpecies");
            monsterSpeciesDatabase = monsterSpeciesArray.ToDictionary(species => species.SpeciesID);
            Debug.Log($"Loaded {monsterSpeciesDatabase.Count} monster species into database.");

            // Tải tất cả EggSO
            var eggArray = Resources.LoadAll<EggSO>("ScriptableObjects/Eggs");
            eggDatabase = eggArray.ToDictionary(egg => egg.EggID);
            Debug.Log($"Loaded {eggDatabase.Count} eggs into database.");
        }

        public MonsterSpeciesSO GetMonsterSpeciesByID(string id)
        {
            monsterSpeciesDatabase.TryGetValue(id, out MonsterSpeciesSO species);
            return species;
        }

        public EggSO GetEggByID(string id)
        {
            eggDatabase.TryGetValue(id, out EggSO egg);
            return egg;
        }
    }
}