using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoTanTuTien.Core;
using VoTanTuTien.Player;
using Sirenix.OdinInspector;

namespace VoTanTuTien.UI
{
    public class StatsUIController : MonoBehaviour
    {
        [Title("UI Element References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private TextMeshProUGUI manaText;

        private CharacterStats targetStats;

        // This method should be called once the player is spawned/initialized
        public void Initialize(PlayerCharacter player)
        {
            if (targetStats != null)
            {
                targetStats.OnHealthChanged -= UpdateHealth;
            }

            targetStats = player.Stats;
            targetStats.OnHealthChanged += UpdateHealth;

            UpdateAllUI();
        }

        private void OnDestroy()
        {
            if (targetStats != null)
            {
                targetStats.OnHealthChanged -= UpdateHealth;
            }
        }

        private void UpdateAllUI()
        {
            UpdateHealth(targetStats.currentHealth, targetStats.maxHealth);
            UpdateMana(targetStats.currentMana, targetStats.maxMana);
        }

        private void UpdateHealth(float current, float max)
        {
            float healthPercent = (max > 0) ? current / max : 0;
            if (healthSlider) healthSlider.value = healthPercent;
            if (healthText) healthText.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";
        }

        private void UpdateMana(float current, float max)
        {
            float manaPercent = (max > 0) ? current / max : 0;
            if (manaSlider) manaSlider.value = manaPercent;
            if (manaText) manaText.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";
        }
    }
}