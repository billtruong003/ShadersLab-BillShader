using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Nebulanook.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        // === DEPENDENCIES ===
        private PlayerFXController fx => PlayerFXController.Instance;
        [FoldoutGroup("Components")][SerializeField] private Rigidbody playerRigidbody;
        [FoldoutGroup("Components")][SerializeField] private PlayerInputHandler playerInput;
        [FoldoutGroup("Components")][SerializeField] private PlayerStamina playerStamina;
        [FoldoutGroup("Components")][SerializeField] private Transform camTransform;

        // === MOVEMENT SETTINGS ===
        [FoldoutGroup("Movement")][SerializeField] private float walkSpeed = 5f;
        [FoldoutGroup("Movement")][SerializeField] private float sprintSpeed = 9f;
        [FoldoutGroup("Movement")][SerializeField] private float rotationSpeed = 20f;

        // === DASH SETTINGS ===
        [FoldoutGroup("Dash")]
        [InfoBox("Dash Force sử dụng ForceMode.Impulse (700-1500+).")]
        [SerializeField] private List<DashTier> dashTiers = new List<DashTier>();
        [FoldoutGroup("Dash")][SerializeField] private float dashDuration = 0.3f;
        [FoldoutGroup("Dash")][SerializeField] private float dashSteeringSpeed = 8f;
        [FoldoutGroup("Dash")][SerializeField] private float knockbackForce = 50f;
        [FoldoutGroup("Dash")][SerializeField][Range(0, 1)] private float minStaminaPercentToDash = 0.2f;

        // === GROUND CHECK ===
        [FoldoutGroup("Ground Check")][SerializeField] private Transform groundCheckTransform;
        [FoldoutGroup("Ground Check")][SerializeField] private float groundCheckRadius = 0.2f;
        [FoldoutGroup("Ground Check")][SerializeField] private LayerMask groundLayer;

        // === FX SETTINGS ===
        [FoldoutGroup("FX")][SerializeField] private float dashImpactKnockbackFXDelay = 1.5f;

        // === STATE VARIABLES ===
        public MovementState CurrentState { get; private set; } = MovementState.Idle;
        public bool IsGrounded { get; private set; }
        public float MaxSpeed => sprintSpeed;

        private float currentChargeTime;
        private float dashTimer;

        // Input flags
        private bool _dashInputDown, _dashInputHeld, _dashInputUp;

        private void Awake()
        {
            if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();
            if (playerInput == null) playerInput = GetComponent<PlayerInputHandler>();
            if (playerStamina == null) playerStamina = GetComponentInChildren<PlayerStamina>();
            if (camTransform == null) camTransform = Camera.main.transform;
        }

        private void Update()
        {
            ReadAndBufferInput();
            HandleDashChargingInput();
        }

        private void FixedUpdate()
        {
            UpdateGroundStatus();
            HandleStateUpdate();
        }

        private void ReadAndBufferInput()
        {
            _dashInputDown = playerInput.DashInputDown;
            _dashInputHeld = playerInput.DashInputHeld;
            _dashInputUp = playerInput.DashInputUp;
        }

        #region State Machine Core

        private void TransitionToState(MovementState newState)
        {
            if (CurrentState == newState) return;

            OnExitState(CurrentState);
            CurrentState = newState;
            OnEnterState(newState);
        }

        private void OnEnterState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Sprinting:
                    fx.Play(PlayerFXID.SprintTrail);
                    fx.Stop(PlayerFXID.FootstepDust); // Đảm bảo tắt hiệu ứng đi bộ
                    break;
                case MovementState.Walking:
                    fx.Play(PlayerFXID.FootstepDust);
                    fx.Stop(PlayerFXID.SprintTrail); // Đảm bảo tắt hiệu ứng chạy nhanh
                    break;
                case MovementState.Charging:
                    currentChargeTime = 0f;
                    playerRigidbody.linearVelocity = Vector3.zero;
                    fx.PlayOneShot(PlayerFXID.DashChargeStart);
                    fx.Play(PlayerFXID.DashChargeLoop);
                    break;
                case MovementState.Dashing:
                    dashTimer = dashDuration;
                    fx.SetActive(PlayerFXID.DashTrail, true);
                    break;
            }
        }

        private void HandleStateUpdate()
        {
            switch (CurrentState)
            {
                case MovementState.Idle:
                case MovementState.Walking:
                case MovementState.Sprinting:
                    HandleLocomotion();
                    HandleRotation();
                    break;
                case MovementState.Dashing:
                    HandleDashing();
                    break;
                case MovementState.Knockback:
                    if (IsGrounded && playerRigidbody.linearVelocity.sqrMagnitude < 1f)
                        TransitionToState(MovementState.Idle);
                    break;
            }
        }

        private void OnExitState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Sprinting:
                    fx.Stop(PlayerFXID.SprintTrail);
                    break;
                case MovementState.Walking:
                    fx.Stop(PlayerFXID.FootstepDust);
                    break;
                case MovementState.Charging:
                    fx.Stop(PlayerFXID.DashChargeLoop);
                    break;
                case MovementState.Dashing:
                    fx.SetActive(PlayerFXID.DashTrail, false);
                    break;
            }
        }

        #endregion

        #region State Logic

        private void HandleLocomotion()
        {
            Vector3 moveDir = CalculateMoveDirection();

            if (moveDir.sqrMagnitude < 0.01f)
            {
                TransitionToState(MovementState.Idle);
                playerRigidbody.linearVelocity = new Vector3(0f, playerRigidbody.linearVelocity.y, 0f);
                return;
            }

            bool wantsSprint = playerInput.SprintInputHeld;
            bool canSprint = wantsSprint && playerStamina.TryDrainStaminaForSprint(Time.fixedDeltaTime);

            float targetSpeed = canSprint ? sprintSpeed : walkSpeed;
            TransitionToState(canSprint ? MovementState.Sprinting : MovementState.Walking);

            Vector3 velocity = moveDir * targetSpeed;
            velocity.y = playerRigidbody.linearVelocity.y;
            playerRigidbody.linearVelocity = velocity;
        }

        private void HandleRotation()
        {
            if (CurrentState == MovementState.Idle) return;
            Vector3 lookDir = CalculateMoveDirection();
            if (lookDir.sqrMagnitude < 0.01f) return;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        private void HandleDashChargingInput()
        {
            bool canStartCharging = IsGrounded &&
                                    CurrentState != MovementState.Charging &&
                                    CurrentState != MovementState.Dashing &&
                                    CurrentState != MovementState.Knockback &&
                                    playerStamina.CurrentStamina > playerStamina.MaxStamina * minStaminaPercentToDash;

            if (_dashInputDown && canStartCharging)
            {
                TransitionToState(MovementState.Charging);
            }

            if (CurrentState == MovementState.Charging)
            {
                currentChargeTime += Time.deltaTime;
                if (_dashInputUp)
                {
                    ExecuteDash();
                }
            }
        }

        private void ExecuteDash()
        {
            DashTier tier = GetDashTierForCurrentCharge();

            if (!playerStamina.TryConsumeStamina(tier.staminaCost))
            {
                fx.PlayOneShot(PlayerFXID.DashChargeEnd);
                TransitionToState(MovementState.Idle);
                return;
            }

            fx.PlayOneShot(PlayerFXID.DashExecute);

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(transform.forward * tier.dashForce, ForceMode.Impulse);

            TransitionToState(MovementState.Dashing);
        }

        private void HandleDashing()
        {
            dashTimer -= Time.fixedDeltaTime;
            SteerWhileDashing();

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            fx.PlayOneShot(PlayerFXID.DashImpact);
            fx.StopAndDeactivateAfterDelay(PlayerFXID.KnockbackHit, dashImpactKnockbackFXDelay);
            playerRigidbody.linearVelocity *= 0.1f;
            ApplyKnockback();
        }

        private void ApplyKnockback()
        {
            playerRigidbody.AddForce(-transform.forward * knockbackForce, ForceMode.Impulse);
            TransitionToState(MovementState.Knockback);
        }

        #endregion

        #region Helper Methods

        private void UpdateGroundStatus()
        {
            IsGrounded = Physics.CheckSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        }

        private Vector3 CalculateMoveDirection()
        {
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            return (camForward.normalized * playerInput.MoveInput.y + camRight.normalized * playerInput.MoveInput.x).normalized;
        }

        private DashTier GetDashTierForCurrentCharge()
        {
            for (int i = dashTiers.Count - 1; i >= 0; i--)
            {
                if (currentChargeTime >= dashTiers[i].chargeTimeRequired)
                    return dashTiers[i];
            }
            return dashTiers.Count > 0 ? dashTiers[0] : default;
        }

        public void ResetMovement()
        {
            TransitionToState(MovementState.Idle);
            playerRigidbody.linearVelocity = Vector3.zero;
            fx.ClearAll();
        }

        private void SteerWhileDashing()
        {
            Vector3 dir = CalculateMoveDirection();
            if (dir.sqrMagnitude > 0.1f)
            {
                float currentSpeed = playerRigidbody.linearVelocity.magnitude;
                Vector3 targetVel = dir * currentSpeed;

                playerRigidbody.linearVelocity = Vector3.Lerp(
                    playerRigidbody.linearVelocity,
                    targetVel,
                    dashSteeringSpeed * Time.fixedDeltaTime);

                transform.rotation = Quaternion.LookRotation(playerRigidbody.linearVelocity.normalized);
            }
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            }
        }
    }

    // Enums and Structs remain the same
    public enum MovementState { Idle, Walking, Sprinting, Charging, Dashing, Knockback }

    [System.Serializable]
    public struct DashTier
    {
        public string tierName;
        public float chargeTimeRequired;
        public float dashForce;
        public float staminaCost;
    }
}