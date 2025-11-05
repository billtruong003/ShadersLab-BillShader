using System.Collections;
using DungeonRush.Core;
using DungeonRush.Inventories;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DungeonRush
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EquipmentManager))]
    public class CombatController : MonoBehaviour
    {
        [Title("Attack Properties")]
        [SerializeField] private float attackRadius = 1.5f;
        [SerializeField] private float attackOffset = 1.0f;
        [SerializeField] private LayerMask damageableLayers;

        [Title("Combat State")]
        [SerializeField, Min(0)] private float comboResetTime = 1.2f;

        [ShowInInspector, ReadOnly]
        [InlineEditor] private WeaponData currentWeapon;

        private PlayerAnimationController animationController;
        private PlayerController playerController;
        private Rigidbody playerRigidbody;
        private EquipmentManager equipmentManager;

        private Coroutine comboResetCoroutine;
        private int comboCounter = 0;
        private bool isAttackBuffered = false;

        public bool IsAttacking { get; private set; }
        [FoldoutGroup("CheatEquip")]
        public WeaponData DataCheat;
        [FoldoutGroup("CheatEquip")]
        public bool EquipFromStart;
        private void Awake()
        {
            animationController = GetComponent<PlayerAnimationController>();
            playerController = GetComponent<PlayerController>();
            playerRigidbody = GetComponent<Rigidbody>();
            equipmentManager = GetComponent<EquipmentManager>();
            if (EquipFromStart && DataCheat != null)
                UpdateWeapon(DataCheat);
        }

        private void OnEnable()
        {
            equipmentManager.OnWeaponEquipped += UpdateWeapon;
        }

        private void OnDisable()
        {
            equipmentManager.OnWeaponEquipped -= UpdateWeapon;
        }

        private void UpdateWeapon(WeaponData newWeapon)
        {
            currentWeapon = newWeapon;
            ResetCombo();
        }

        public void ProcessAttackRequest()
        {
            if (currentWeapon == null) return;

            if (IsAttacking)
            {
                BufferAttack();
            }
            else
            {
                PerformAttack();
            }
        }

        private void BufferAttack()
        {
            if (comboCounter < currentWeapon.comboSteps.Length)
            {
                isAttackBuffered = true;
            }
        }

        private void PerformAttack()
        {
            if (comboCounter >= currentWeapon.comboSteps.Length) return;

            IsAttacking = true;
            isAttackBuffered = false;

            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);

            Vector3 attackDirection = GetAttackDirection();
            transform.rotation = Quaternion.LookRotation(attackDirection);

            ComboStep currentStep = currentWeapon.comboSteps[comboCounter];
            animationController.TriggerAttack(currentStep.animationID);

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(attackDirection * currentStep.lungeForce, ForceMode.Impulse);

            comboCounter++;
        }

        private Vector3 GetAttackDirection()
        {
            return playerController.InputDirection == Vector3.zero ?
                   transform.forward :
                   playerController.InputDirection;
        }

        // --- METHOD NÀY SẼ ĐƯỢC GỌI TỪ ANIMATION EVENT ---
        public void DealDamageToTargets()
        {
            if (currentWeapon == null) return;

            Vector3 attackCenter = transform.position + transform.forward * attackOffset;
            Collider[] hitTargets = Physics.OverlapSphere(attackCenter, attackRadius, damageableLayers);

            float baseDamage = currentWeapon.baseDamage;
            float damageMultiplier = currentWeapon.comboSteps[comboCounter - 1].damageMultiplier;
            float totalDamage = baseDamage * damageMultiplier;

            foreach (var target in hitTargets)
            {
                if (target.TryGetComponent<HealthComponent>(out var health))
                {
                    health.TakeDamage(totalDamage);
                }
            }
        }

        // --- METHOD NÀY SẼ ĐƯỢC GỌI TỪ ANIMATION EVENT ---
        public void FinalizeAttackState()
        {
            IsAttacking = false;

            if (isAttackBuffered)
            {
                PerformAttack();
            }
            else
            {
                comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());
            }
        }

        private IEnumerator ResetComboAfterDelay()
        {
            yield return new WaitForSeconds(comboResetTime);
            ResetCombo();
        }

        private void ResetCombo()
        {
            comboCounter = 0;
            isAttackBuffered = false;
            animationController.ResetCombatAnimation();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 attackCenter = transform.position + transform.forward * attackOffset;
            Gizmos.DrawWireSphere(attackCenter, attackRadius);
        }
    }
}