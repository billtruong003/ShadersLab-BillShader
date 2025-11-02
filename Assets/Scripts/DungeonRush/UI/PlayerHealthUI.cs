using DungeonRush.Core;
using UnityEngine;

namespace DungeonRush.UI
{
    [RequireComponent(typeof(UIBillProgress))]
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealthComponent;
        [SerializeField] private float animationDuration = 0.5f;
        private UIBillProgress healthBar;
        private LTDescr healthTween;

        private void Awake()
        {
            healthBar = GetComponent<UIBillProgress>();
            if (playerHealthComponent == null)
            {
                playerHealthComponent = FindFirstObjectByType<PlayerController>()?.GetComponent<HealthComponent>();
            }
        }

        private void OnEnable()
        {
            if (playerHealthComponent != null)
            {
                playerHealthComponent.OnHealthChanged += UpdateHealthBar;
                InitializeHealthBar();
            }
        }

        private void OnDisable()
        {
            if (playerHealthComponent != null)
            {
                playerHealthComponent.OnHealthChanged -= UpdateHealthBar;
            }
        }

        private void InitializeHealthBar()
        {
            healthBar.SetProgress(playerHealthComponent.CurrentHealth, playerHealthComponent.MaxHealth);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthTween != null)
            {
                LeanTween.cancel(healthTween.id);
            }

            float targetNormalizedValue = (maxHealth > 0) ? (currentHealth / maxHealth) : 0;

            healthTween = LeanTween.value(gameObject, healthBar.GetCurrentFill(), targetNormalizedValue, animationDuration)
                .setOnUpdate(healthBar.SetNormalizedProgress)
                .setEase(LeanTweenType.easeOutCubic);
        }
    }
}