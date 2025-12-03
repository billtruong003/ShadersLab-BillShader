using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Shmackle.Utils.CoroutinesTimer; // Thêm using để sử dụng tiện ích

namespace Nebulanook.Player
{
    [RequireComponent(typeof(BillProgress))]
    public class PlayerStamina : MonoBehaviour
    {
        [Title("Stamina Settings")]
        [ProgressBar(0, "maxStamina", Height = 30)]
        [SerializeField] private float currentStamina;
        [SerializeField] private float maxStamina = 100f;

        [Title("Rates")]
        [SerializeField] private float sprintStaminaDrainRate = 15f;
        [SerializeField] private float regenRate = 20f;
        [SerializeField] private float regenDelay = 1.5f;

        private BillProgress staminaBarUI;
        private Coroutine regenCoroutine;

        // Đã loại bỏ biến "regenTick" để sử dụng CoroutineTimeUtils
        private const float REGEN_TICK_INTERVAL = 0.1f;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;

        private void Awake()
        {
            staminaBarUI = GetComponent<BillProgress>();
            currentStamina = maxStamina;
            UpdateStaminaUI();
        }

        public bool TryConsumeStamina(float amount)
        {
            if (currentStamina < amount)
            {
                return false;
            }

            currentStamina -= amount;
            UpdateStaminaUI();

            StopAndRestartRegeneration();
            return true;
        }

        public bool TryDrainStaminaForSprint(float deltaTime)
        {
            return TryConsumeStamina(sprintStaminaDrainRate * deltaTime);
        }

        private void StopAndRestartRegeneration()
        {
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
            }
            regenCoroutine = StartCoroutine(RegenerateStamina());
        }

        private IEnumerator RegenerateStamina()
        {
            // Thay thế new WaitForSeconds() bằng CoroutineTimeUtils
            yield return CoroutineTimeUtils.GetWaitForSeconds(regenDelay);

            while (currentStamina < maxStamina)
            {
                currentStamina += regenRate * REGEN_TICK_INTERVAL;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                UpdateStaminaUI();

                yield return CoroutineTimeUtils.GetWaitForSeconds(REGEN_TICK_INTERVAL);
            }
            regenCoroutine = null;
        }

        private void UpdateStaminaUI()
        {
            staminaBarUI.SetProgress(currentStamina, maxStamina);
        }
    }
}