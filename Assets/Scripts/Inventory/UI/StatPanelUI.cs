// Path: Assets/Scripts/UI/StatsPanelUI.cs
using UnityEngine;
using TMPro;

public class StatsPanelUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [Header("Stat Text Fields")]
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI cooldownReductionText;
    [SerializeField] private TextMeshProUGUI areaSizeText;
    [SerializeField] private TextMeshProUGUI xpGainText;
    // Thêm các TextMeshProUGUI khác cho các chỉ số bạn muốn hiển thị

    private void Start()
    {
        playerStats.OnStatsChanged += UpdateAllStatDisplays;
        UpdateAllStatDisplays();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateAllStatDisplays;
        }
    }

    private void UpdateAllStatDisplays()
    {
        UpdateSingleStat(maxHealthText, "Max Health", playerStats.GetStat(StatType.MaxHealth), false);
        UpdateSingleStat(damageText, "Damage", playerStats.GetStat(StatType.Damage), true);
        UpdateSingleStat(moveSpeedText, "Move Speed", playerStats.GetStat(StatType.MoveSpeed), false);
        UpdateSingleStat(cooldownReductionText, "Cooldown", playerStats.GetStat(StatType.Cooldown), true);
        UpdateSingleStat(areaSizeText, "Area Size", playerStats.GetStat(StatType.AreaSize), true);
        UpdateSingleStat(xpGainText, "XP Gain", playerStats.GetStat(StatType.XPGain), true);
    }

    private void UpdateSingleStat(TextMeshProUGUI textElement, string label, float value, bool isPercentage)
    {
        if (textElement == null) return;

        if (isPercentage)
        {
            textElement.text = $"{label}: {value:P0}"; // P0 định dạng thành % không có số thập phân
        }
        else
        {
            textElement.text = $"{label}: {value:F1}"; // F1 định dạng số có 1 chữ số thập phân
        }
    }
}