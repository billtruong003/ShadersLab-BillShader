using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Nebulanook.Core;

namespace Nebulanook.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        private PlayerFXController fx => PlayerFXController.Instance;
        [FoldoutGroup("Components")][SerializeField] private Rigidbody playerRigidbody;
        [FoldoutGroup("Components")][SerializeField] private PlayerInputHandler playerInput;
        [FoldoutGroup("Components")][SerializeField] private PlayerStamina playerStamina;
        [FoldoutGroup("Components")][SerializeField] private Transform camTransform;

        [FoldoutGroup("Movement")][SerializeField] private float walkSpeed = 5f;
        [FoldoutGroup("Movement")][SerializeField] private float sprintSpeed = 9f;
        [FoldoutGroup("Movement")][SerializeField] private float rotationSpeed = 20f;

        [FoldoutGroup("Dash")]
        [SerializeField] private List<DashTier> dashTiers = new List<DashTier>();
        [FoldoutGroup("Dash")][SerializeField] private float dashDuration = 0.3f;
        [FoldoutGroup("Dash")][SerializeField] private float dashSteeringSpeed = 8f;
        [FoldoutGroup("Dash")][SerializeField] private float knockbackForce = 30f;
        [FoldoutGroup("Dash")][SerializeField] private float knockbackStunDuration = 0.5f;
        [FoldoutGroup("Dash")][SerializeField][Range(0, 1)] private float minStaminaPercentToDash = 0.2f;
        [FoldoutGroup("Dash")][SerializeField] private LayerMask collisionLayer;

        [FoldoutGroup("Feedback")][SerializeField] private float collisionShakeForce = 1.5f;
        [FoldoutGroup("Feedback")][SerializeField] private float collisionShakeDuration = 0.3f;

        [FoldoutGroup("Ground Check")][SerializeField] private Transform groundCheckTransform;
        [FoldoutGroup("Ground Check")][SerializeField] private float groundCheckRadius = 0.2f;
        [FoldoutGroup("Ground Check")][SerializeField] private LayerMask groundLayer;

        public MovementState CurrentState { get; private set; } = MovementState.Idle;
        public bool IsGrounded { get; private set; }
        public float MaxSpeed => sprintSpeed;

        private float currentChargeTime;
        private float dashTimer;
        private float stunTimer;
        private DashTier currentTier;
        private bool isControlLocked;

        private void Awake()
        {
            if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();
            if (playerInput == null) playerInput = GetComponent<PlayerInputHandler>();
            if (playerStamina == null) playerStamina = GetComponentInChildren<PlayerStamina>();
            if (camTransform == null) camTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(bool isLocked)
        {
            isControlLocked = isLocked;
            playerInput.SetInputActive(!isLocked);

            if (isLocked)
            {
                TransitionToState(MovementState.Idle);
                playerRigidbody.linearVelocity = Vector3.zero;
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundStatus();
            HandleStateUpdate();
        }

        private void Update()
        {
            if (isControlLocked) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (CurrentState == MovementState.Knockback) return;

            bool canStartCharging = IsGrounded &&
                                    CurrentState != MovementState.Charging &&
                                    CurrentState != MovementState.Dashing &&
                                    playerStamina.CurrentStamina > playerStamina.MaxStamina * minStaminaPercentToDash;

            if (playerInput.DashInputDown && canStartCharging)
            {
                TransitionToState(MovementState.Charging);
            }

            if (CurrentState == MovementState.Charging)
            {
                currentChargeTime += Time.deltaTime;
                if (playerInput.DashInputUp)
                {
                    ExecuteDash();
                }
            }
        }

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
                    fx.Stop(PlayerFXID.FootstepDust);
                    break;
                case MovementState.Walking:
                    fx.Play(PlayerFXID.FootstepDust);
                    fx.Stop(PlayerFXID.SprintTrail);
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
                case MovementState.Knockback:
                    stunTimer = knockbackStunDuration;
                    fx.PlayOneShot(PlayerFXID.KnockbackHit);
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

        private void HandleStateUpdate()
        {
            if (isControlLocked) return;

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
                    HandleKnockback();
                    break;
            }
        }

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
            Vector3 lookDir = CalculateMoveDirection();
            if (lookDir.sqrMagnitude < 0.01f) return;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        private void ExecuteDash()
        {
            currentTier = GetDashTierForCurrentCharge();

            if (!playerStamina.TryConsumeStamina(currentTier.staminaCost))
            {
                fx.PlayOneShot(PlayerFXID.DashChargeEnd);
                TransitionToState(MovementState.Idle);
                return;
            }

            fx.PlayOneShot(PlayerFXID.DashExecute);
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(transform.forward * currentTier.dashForce, ForceMode.Impulse);
            TransitionToState(MovementState.Dashing);
        }

        private void HandleDashing()
        {
            dashTimer -= Time.fixedDeltaTime;
            SteerWhileDashing();
            if (dashTimer <= 0f)
            {
                TransitionToState(MovementState.Idle);
                playerRigidbody.linearVelocity *= 0.5f;
            }
        }

        private void SteerWhileDashing()
        {
            Vector3 dir = CalculateMoveDirection();
            if (dir.sqrMagnitude > 0.1f)
            {
                float currentSpeed = playerRigidbody.linearVelocity.magnitude;
                Vector3 targetVel = dir * currentSpeed;
                playerRigidbody.linearVelocity = Vector3.Lerp(playerRigidbody.linearVelocity, targetVel, dashSteeringSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.LookRotation(playerRigidbody.linearVelocity.normalized);
            }
        }

        private void HandleKnockback()
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0f && IsGrounded)
            {
                TransitionToState(MovementState.Idle);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (CurrentState != MovementState.Dashing) return;
            if (((1 << collision.gameObject.layer) & collisionLayer) == 0) return;

            IBumpable bumpable = collision.gameObject.GetComponent<IBumpable>();
            if (bumpable != null)
            {
                float impactForce = currentTier.dashForce * 0.5f;
                bumpable.OnBump(transform.forward, impactForce);
            }

            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.Shake(collisionShakeDuration, collisionShakeForce);
            }

            Vector3 normal = collision.contacts[0].normal;
            Vector3 reflection = Vector3.Reflect(transform.forward, normal);

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(reflection * knockbackForce + Vector3.up * (knockbackForce * 0.2f), ForceMode.Impulse);

            TransitionToState(MovementState.Knockback);
        }

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
                if (currentChargeTime >= dashTiers[i].chargeTimeRequired) return dashTiers[i];
            }
            return dashTiers.Count > 0 ? dashTiers[0] : default;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            }
        }
    }

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