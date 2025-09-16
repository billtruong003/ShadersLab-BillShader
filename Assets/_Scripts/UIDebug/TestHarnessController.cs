using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using MonsterLegion.Core;
using MonsterLegion.Data;
using MonsterLegion.Gameplay; // Cần cho StageSO

// File: Assets/_Scripts/UI/Debug/TestHarnessController.cs
public class TestHarnessController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _monsterListContainer;
    [SerializeField] private GameObject _monsterItemPrefab;

    [Header("Combat Configuration")]
    [SerializeField] private StageSO _stageToLoad; // Kéo StageSO bạn muốn test vào đây

    void Start()
    {
        // Khi scene bắt đầu, kiểm tra xem đã có dữ liệu chưa
        if (GameDataManager.Instance.Data == null)
        {
            // Nếu chưa, có thể là lần chạy đầu tiên, tải game hoặc tạo mới
            GameDataManager.Instance.LoadGame();
        }
        // Luôn làm mới UI để hiển thị dữ liệu mới nhất
        RefreshMonsterListDisplay();
    }

    public void CreateNewGameWithDefaults()
    {
        // Tạo một game mới hoàn toàn
        GameDataManager.Instance.CreateNewGame();

        // Thêm một vài quái vật mẫu để test ngay lập tức
        PlayerData data = GameDataManager.Instance.Data;
        data.OwnedMonsters.Add(new MonsterInstance("fire_golem_01", Personality.Brave));
        data.OwnedMonsters.Add(new MonsterInstance("stone_golem_01", Personality.Cautious));
        data.OwnedMonsters.Add(new MonsterInstance("wind_archer_01", Personality.Reckless));
        data.OwnedMonsters.Add(new MonsterInstance("water_priestess_01", Personality.Neutral));
        data.OwnedMonsters.Add(new MonsterInstance("dark_assassin_01", Personality.Brave));
        data.OwnedMonsters.Add(new MonsterInstance("light_paladin_01", Personality.Timid));

        Debug.Log("Đã tạo game mới với 6 quái vật mẫu.");

        // Làm mới giao diện để hiển thị các quái vật vừa được thêm
        RefreshMonsterListDisplay();
    }

    private void RefreshMonsterListDisplay()
    {
        // Xóa tất cả các item cũ trong danh sách
        foreach (Transform child in _monsterListContainer)
        {
            Destroy(child.gameObject);
        }

        // Lấy danh sách quái vật hiện tại
        List<MonsterInstance> ownedMonsters = GameDataManager.Instance.Data.OwnedMonsters;

        // Tạo lại các item UI cho từng quái vật
        foreach (MonsterInstance monster in ownedMonsters)
        {
            GameObject itemGO = Instantiate(_monsterItemPrefab, _monsterListContainer);
            itemGO.GetComponent<MonsterSelectionUIItem>().Initialize(monster);
        }
    }

    public void StartCombat()
    {
        if (_stageToLoad == null)
        {
            Debug.LogError("Chưa chọn StageSO để test! Hãy kéo một Stage vào field 'Stage To Load' trên TestHarnessController.");
            return;
        }

        // Bước 1: Lưu lại tất cả các thay đổi (chọn quái vật)
        GameDataManager.Instance.SaveGame();

        // Bước 2: Truyền dữ liệu màn chơi cần tải
        // Đây là cách đơn giản và hiệu quả để truyền dữ liệu giữa các scene
        // mà không cần một manager phức tạp hơn.
        StaticStageHolder.StageToLoad = _stageToLoad;

        // Bước 3: Tải scene chiến đấu
        SceneManager.LoadScene("Combat"); // Đảm bảo tên scene là "Combat"
    }
}

// Lớp static đơn giản để giữ tham chiếu StageSO khi chuyển scene
public static class StaticStageHolder
{
    public static StageSO StageToLoad { get; set; }
}