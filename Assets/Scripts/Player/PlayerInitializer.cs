using UnityEngine;
using System.Collections.Generic;

public class PlayerInitializer : MonoBehaviour
{
    [SerializeField] private List<EquipmentItemData> startingEquipment;
    [SerializeField] private PlayerStats playerStats; // Vẫn cần tham chiếu này

    void Start()
    {
        // Đăng ký PlayerStats với manager trung tâm
        GameDataManager.Instance.RegisterPlayerStats(playerStats);

        // Giờ mới khởi tạo trang bị
        InitializeEquipment();
    }

    private void InitializeEquipment()
    {
        var equipmentSystem = GameDataManager.Instance.EquipmentSystem;
        foreach (var item in startingEquipment)
        {
            equipmentSystem.Equip(item);
        }
    }
}