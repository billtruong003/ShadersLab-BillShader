using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MonsterLegion.Data;
using MonsterLegion.Core;
using System;

// File: Assets/_Scripts/UI/Debug/MonsterSelectionUIItem.cs
public class MonsterSelectionUIItem : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameLevelText;
    [SerializeField] private Toggle _isInSquadToggle;

    private MonsterInstance _monsterInstance;

    // Hàm này được gọi từ TestHarnessController để gán dữ liệu
    public void Initialize(MonsterInstance monsterInstance)
    {
        _monsterInstance = monsterInstance;

        // Lấy dữ liệu tĩnh từ database để hiển thị
        MonsterSpeciesSO species = GameDatabase.Instance.GetMonsterSpeciesByID(monsterInstance.SpeciesID);
        if (species != null)
        {
            _iconImage.sprite = species.Icon;
            _nameLevelText.text = $"{species.DisplayName} - Lv.{monsterInstance.CurrentLevel}";
        }

        // Cập nhật trạng thái của Toggle dựa trên dữ liệu hiện tại
        _isInSquadToggle.isOn = _monsterInstance.IsInEliteSquad;

        // Xóa các listener cũ để tránh gọi nhiều lần, sau đó thêm listener mới
        _isInSquadToggle.onValueChanged.RemoveAllListeners();
        _isInSquadToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    // Khi người dùng nhấn vào Toggle
    private void OnToggleChanged(bool isSelected)
    {
        // Cập nhật trực tiếp vào data object
        _monsterInstance.IsInEliteSquad = isSelected;
        Debug.Log($"Đã cập nhật {_monsterInstance.SpeciesID} (ID: {_monsterInstance.InstanceID}) IsInSquad = {isSelected}");
    }
}