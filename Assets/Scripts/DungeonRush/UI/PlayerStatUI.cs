using DungeonRush.Stats;
using TMPro;
using UnityEngine;
using DungeonRush.Core;

namespace DungeonRush.UI
{
    public class PlayerStatsUI : MonoBehaviour
    {
        [Header("Stat Text Fields")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private TextMeshProUGUI walkSpeedText;
        [SerializeField] private TextMeshProUGUI runSpeedText;

        [Header("Target")]
        [SerializeField] private PlayerStatController playerStatController;

        private void OnEnable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsChanged += UpdateStatDisplay;
            }
            UpdateStatDisplay();
        }

        private void OnDisable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsChanged -= UpdateStatDisplay;
            }
        }

        private void UpdateStatDisplay()
        {
            if (playerStatController == null) return;

            SetText(damageText, playerStatController.GetStat(Core.StatType.Damage));
            SetText(defenseText, playerStatController.GetStat(Core.StatType.Defense));
            SetText(walkSpeedText, playerStatController.GetStat(Core.StatType.WalkSpeed));
            SetText(runSpeedText, playerStatController.GetStat(Core.StatType.RunSpeed));
        }

        private void SetText(TextMeshProUGUI textField, float value)
        {
            if (textField != null)
            {
                textField.text = value.ToString("F1");
            }
        }
    }
}