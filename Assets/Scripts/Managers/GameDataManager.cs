using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    public InventorySystem InventorySystem { get; private set; }
    public EquipmentSystem EquipmentSystem { get; private set; }

    // Giữ tham chiếu đến PlayerStats để các hệ thống khác có thể truy cập
    // mà không cần FindObjectOfType
    private PlayerStats currentPlayerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSystems();
    }

    private void InitializeSystems()
    {
        InventorySystem = new InventorySystem(30); // Ví dụ: 30 ô đồ
    }

    // Player sẽ gọi hàm này khi được khởi tạo để đăng ký Stats của mình
    public void RegisterPlayerStats(PlayerStats stats)
    {
        currentPlayerStats = stats;
        // Khởi tạo EquipmentSystem sau khi đã có PlayerStats
        EquipmentSystem = new EquipmentSystem(InventorySystem, currentPlayerStats);
    }

    public PlayerStats GetCurrentPlayerStats()
    {
        return currentPlayerStats;
    }
}