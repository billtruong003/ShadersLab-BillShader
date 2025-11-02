using DungeonRush.Core;
using DungeonRush.Stats;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace DungeonRush
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CombatController))]
    [RequireComponent(typeof(PlayerAnimationController))]
    [RequireComponent(typeof(PlayerStatController))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerStatController statController;
        private Rigidbody playerRigidbody;
        private CombatController combatController;
        private PlayerAnimationController animationController;

        private bool isDashing;
        private bool canDash = true;

        public Vector3 InputDirection { get; private set; }

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            GatherInput();
            HandleRotation();
            UpdateAnimations();
            HandleDashRequest();
            HandleAttackRequest();
        }

        private void FixedUpdate()
        {
            HandlePhysicsBasedMovement();
        }

        private void InitializeComponents()
        {
            statController = GetComponent<PlayerStatController>();
            playerRigidbody = GetComponent<Rigidbody>();
            combatController = GetComponent<CombatController>();
            animationController = GetComponent<PlayerAnimationController>();
            playerRigidbody.freezeRotation = true;
        }

        private void GatherInput()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            InputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        }

        private bool CanPerformMovement() => !isDashing && !combatController.IsAttacking;
        private bool CanPerformAction() => !isDashing;

        private void HandlePhysicsBasedMovement()
        {
            if (!CanPerformMovement()) return;

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ?
                                  statController.GetStat(Core.StatType.RunSpeed) :
                                  statController.GetStat(Core.StatType.WalkSpeed);

            Vector3 targetVelocity = InputDirection * targetSpeed;
            Vector3 velocityDifference = targetVelocity - playerRigidbody.linearVelocity;
            velocityDifference.y = 0;

            float moveForce = statController.GetStat(Core.StatType.MoveForce);
            playerRigidbody.AddForce(velocityDifference * moveForce, ForceMode.Force);
        }

        private void HandleRotation()
        {
            if (InputDirection == Vector3.zero || !CanPerformMovement()) return;

            Quaternion targetRotation = Quaternion.LookRotation(InputDirection);
            float rotationSpeed = statController.GetStat(Core.StatType.RotationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimations()
        {
            float currentSpeed = new Vector3(playerRigidbody.linearVelocity.x, 0, playerRigidbody.linearVelocity.z).magnitude;
            float runSpeed = statController.GetStat(Core.StatType.RunSpeed);
            float normalizedSpeed = runSpeed > 0 ? Mathf.Clamp01(currentSpeed / runSpeed) : 0f;
            animationController.UpdateMovementAnimation(normalizedSpeed);
        }

        private void HandleDashRequest()
        {
            if (Input.GetKeyDown(KeyCode.Space) && canDash && CanPerformAction() && InputDirection != Vector3.zero)
            {
                StartCoroutine(PerformDash());
            }
        }

        private void HandleAttackRequest()
        {
            if (Input.GetKeyDown(KeyCode.J) && CanPerformAction())
            {
                combatController.ProcessAttackRequest();
            }
        }

        private IEnumerator PerformDash()
        {
            isDashing = true;
            canDash = false;

            float dashForce = statController.GetStat(Core.StatType.DashForce);
            float dashDuration = statController.GetStat(Core.StatType.DashDuration);
            float dashCooldown = statController.GetStat(Core.StatType.DashCooldown);

            animationController.TriggerDash();
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(InputDirection * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);
            isDashing = false;

            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }
    }
}