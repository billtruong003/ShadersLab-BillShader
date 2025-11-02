using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using DungeonRush.UI; // Thêm namespace này để sử dụng UIBillProgress

namespace DungeonRush.Core
{
    public class HealthComponent : MonoBehaviour
    {
        public enum HealthBarType
        {
            None,
            UIBillProgress,
            WorldBillProgress
        }

        [Title("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [ShowInInspector, ReadOnly] private bool isInvincible = false;

        [Title("Health Bar")]
        [SerializeField] private HealthBarType healthBarType = HealthBarType.None;

        [ShowIf("healthBarType", HealthBarType.UIBillProgress)]
        [SerializeField] private UIBillProgress uiHealthBar;

        [ShowIf("healthBarType", HealthBarType.WorldBillProgress)]
        [SerializeField] private BillProgress worldHealthBar;

        [ShowIf("healthBarType", HealthBarType.WorldBillProgress)]
        [Tooltip("Đối tượng cha của thanh máu world-space để xoay và ẩn/hiện.")]
        [SerializeField] private GameObject worldHealthBarParent;

        [ShowIf("healthBarType", HealthBarType.WorldBillProgress)]
        [SerializeField] private float worldBarDisplayDuration = 3f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        private Coroutine regenerationCoroutine;
        private Coroutine hideWorldBarCoroutine;
        private Transform cameraTransform;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            // Khởi tạo trạng thái ban đầu của thanh máu
            UpdateHealthBarVisual(CurrentHealth, maxHealth);
            if (worldHealthBarParent != null)
            {
                worldHealthBarParent.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            // Billboarding cho thanh máu world-space
            if (healthBarType == HealthBarType.WorldBillProgress && worldHealthBarParent.activeSelf && cameraTransform != null)
            {
                worldHealthBarParent.transform.LookAt(cameraTransform);
            }
        }

        public void TakeDamage(float damageAmount)
        {
            if (damageAmount <= 0 || isInvincible || IsDead) return;

            CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateHealthBarVisual(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            if (healAmount <= 0 || IsDead) return;

            CurrentHealth = Mathf.Min(CurrentHealth + healAmount, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateHealthBarVisual(CurrentHealth, maxHealth);
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDeath?.Invoke();

            if (worldHealthBarParent != null) HideWorldBarImmediately();

            Debug.Log($"{gameObject.name} has died.");
        }

        private void UpdateHealthBarVisual(float current, float max)
        {
            switch (healthBarType)
            {
                case HealthBarType.UIBillProgress:
                    if (uiHealthBar != null) uiHealthBar.SetProgress(current, max);
                    break;

                case HealthBarType.WorldBillProgress:
                    if (worldHealthBar != null)
                    {
                        worldHealthBar.SetProgress(current, max);
                        // Chỉ hiện khi máu không đầy và chưa chết
                        if (current < max && !IsDead)
                        {
                            ShowAndAutoHideWorldBar();
                        }
                    }
                    break;
            }
        }

        private void ShowAndAutoHideWorldBar()
        {
            if (hideWorldBarCoroutine != null) StopCoroutine(hideWorldBarCoroutine);
            if (worldHealthBarParent != null) worldHealthBarParent.SetActive(true);
            hideWorldBarCoroutine = StartCoroutine(HideWorldBarAfterDelay());
        }

        private void HideWorldBarImmediately()
        {
            if (hideWorldBarCoroutine != null) StopCoroutine(hideWorldBarCoroutine);
            if (worldHealthBarParent != null) worldHealthBarParent.SetActive(false);
        }

        private IEnumerator HideWorldBarAfterDelay()
        {
            yield return new WaitForSeconds(worldBarDisplayDuration);
            if (worldHealthBarParent != null) worldHealthBarParent.SetActive(false);
        }

        #region Odin Debug Tools
        [FoldoutGroup("Debugging Cheats")]
        [Button("Test Take Damage (10)"), GUIColor(0.9f, 0.4f, 0.4f)]
        private void TestTakeDamage() => TakeDamage(10);

        [FoldoutGroup("Debugging Cheats")]
        [Button("Force Die"), GUIColor(0.6f, 0.2f, 0.2f)]
        private void TriggerDie() => Die();

        [FoldoutGroup("Debugging Cheats")]
        [Button("Toggle Invincibility")]
        private void ToggleInvincibility() => isInvincible = !isInvincible;
        #endregion
    }
}