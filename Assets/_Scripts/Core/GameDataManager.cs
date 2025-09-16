using UnityEngine;
using System.IO;
using MonsterLegion.Data;

namespace MonsterLegion.Core
{
    public class GameDataManager : MonoBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        public PlayerData Data { get; private set; }

        private const string SAVE_FILE_NAME = "playerdata.json";
        private string saveFilePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

            LoadGame();
        }

        public void LoadGame()
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                Data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("Game loaded successfully from: " + saveFilePath);
            }
            else
            {
                CreateNewGame();
                Debug.Log("No save file found. Creating a new game.");
            }
        }

        public void CreateNewGame()
        {
            Data = new PlayerData("Commander"); // Tên mặc định
            SaveGame();
        }

        public void SaveGame()
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log("Game saved successfully to: " + saveFilePath);
        }

        public MonsterInstance GetMonsterByID(long instanceID)
        {
            return Data.OwnedMonsters.Find(monster => monster.InstanceID == instanceID);
        }
    }
}