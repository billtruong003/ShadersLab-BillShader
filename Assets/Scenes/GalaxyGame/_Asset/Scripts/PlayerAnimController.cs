using UnityEngine;
using Sirenix.OdinInspector;

namespace Nebulanook.Player
{
    [RequireComponent(typeof(Animator), typeof(PlayerMovement), typeof(Rigidbody))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator playerAnimator;
        private PlayerMovement playerMovement;
        private Rigidbody playerRigidbody;

        // --- HASHES ---
        // Loại bỏ isSprintingHash
        private readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int isChargingHash = Animator.StringToHash("IsCharging");
        private readonly int executeDashHash = Animator.StringToHash("ExecuteDash");
        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            playerAnimator = GetComponent<Animator>();
            playerMovement = GetComponent<PlayerMovement>();
            playerRigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            UpdateMovementAnimation();
            UpdateStateAnimation();
        }

        private void UpdateMovementAnimation()
        {
            Vector3 horizontalVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0, playerRigidbody.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            float maxSpeed = playerMovement.MaxSpeed;
            float normalizedSpeed = maxSpeed > 0 ? currentSpeed / maxSpeed : 0f;
            playerAnimator.SetFloat(moveSpeedHash, normalizedSpeed);
            playerAnimator.SetBool(isGroundedHash, playerMovement.IsGrounded);
        }

        private void UpdateStateAnimation()
        {
            bool isCharging = playerMovement.CurrentState == MovementState.Charging;
            playerAnimator.SetBool(isChargingHash, isCharging);

            if (playerMovement.CurrentState == MovementState.Dashing && playerAnimator.GetBool(isChargingHash))
            {
                playerAnimator.SetTrigger(executeDashHash);
            }
        }
    }
}