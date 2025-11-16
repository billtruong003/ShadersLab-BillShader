using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;

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
        private WaitForSeconds regenTick = new WaitForSeconds(0.1f);

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

            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
            }
            regenCoroutine = StartCoroutine(RegenerateStamina());
            return true;
        }

        // --- THAY ĐỔI Ở ĐÂY ---
        // Chuyển từ void sang bool để trả về kết quả thành công/thất bại
        public bool TryDrainStaminaForSprint(float deltaTime)
        {
            return TryConsumeStamina(sprintStaminaDrainRate * deltaTime);
        }

        private IEnumerator RegenerateStamina()
        {
            yield return new WaitForSeconds(regenDelay);

            while (currentStamina < maxStamina)
            {
                currentStamina += regenRate * 0.1f;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                UpdateStaminaUI();
                yield return regenTick;
            }
            regenCoroutine = null;
        }

        private void UpdateStaminaUI()
        {
            staminaBarUI.SetProgress(currentStamina, maxStamina);
        }
    }
}