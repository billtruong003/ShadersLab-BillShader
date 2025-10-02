// Path: Assets/Scripts/Managers/UpgradeManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private LevelingSystem levelingSystem;
    [SerializeField] private UpgradeChoiceUI upgradeChoiceUI;
    [SerializeField] private List<UpgradeData> allUpgradesPool;
    [SerializeField] private int choicesPerLevelUp = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        levelingSystem.OnLevelUp += PresentUpgradeChoices;
    }

    private void OnDisable()
    {
        levelingSystem.OnLevelUp -= PresentUpgradeChoices;
    }

    private void PresentUpgradeChoices()
    {
        Time.timeScale = 0f; // Dừng game
        var randomChoices = GetRandomUpgrades(choicesPerLevelUp);
        upgradeChoiceUI.DisplayChoices(randomChoices);
    }

    public void SelectUpgrade(UpgradeData chosenUpgrade)
    {
        playerStats.ApplyUpgrade(chosenUpgrade);
        upgradeChoiceUI.Hide();
        Time.timeScale = 1f; // Tiếp tục game
    }

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        if (allUpgradesPool.Count <= count)
        {
            return new List<UpgradeData>(allUpgradesPool);
        }

        // Tạm thời chọn ngẫu nhiên. Sau này có thể thêm logic phức tạp hơn
        // (ví dụ: ưu tiên nâng cấp cho vũ khí đang cầm)
        return allUpgradesPool.OrderBy(x => Random.value).Take(count).ToList();
    }
}