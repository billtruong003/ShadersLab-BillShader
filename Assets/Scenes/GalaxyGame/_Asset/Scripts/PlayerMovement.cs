using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;

namespace Nebulanook.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        [FoldoutGroup("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [FoldoutGroup("Movement Settings")]
        [SerializeField] private float sprintSpeed = 9f;
        [FoldoutGroup("Movement Settings")]
        [SerializeField] private float rotationSpeed = 20f;

        [FoldoutGroup("Dash Settings")]
        [InfoBox("Dash Force sử dụng ForceMode.Impulse, cần giá trị lớn (vd: 700-1500) để có hiệu quả.")]
        [SerializeField] private List<DashTier> dashTiers;
        [FoldoutGroup("Dash Settings")]
        [SerializeField] private float dashDuration = 0.3f;
        [FoldoutGroup("Dash Settings")]
        [SerializeField] private float dashSteeringSpeed = 8f; // Tốc độ bẻ lái khi đang húc
        [FoldoutGroup("Dash Settings")]
        [SerializeField] private float knockbackForce = 50f;

        [FoldoutGroup("Ground Check")]
        [SerializeField] private Transform groundCheckTransform;
        [FoldoutGroup("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.2f;
        [FoldoutGroup("Ground Check")]
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody playerRigidbody;
        private PlayerInputHandler playerInput;
        private PlayerStamina playerStamina;
        private Transform mainCameraTransform;

        private float currentChargeTime;
        private float dashTimer;

        public MovementState CurrentState { get; private set; } = MovementState.Idle;
        public bool IsGrounded { get; private set; }
        public float MaxSpeed => sprintSpeed;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            playerInput = GetComponent<PlayerInputHandler>();
            playerStamina = GetComponentInChildren<PlayerStamina>();
            mainCameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (IsGrounded && CurrentState != MovementState.Dashing && CurrentState != MovementState.Knockback)
            {
                HandleDashInput();
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundStatus();

            switch (CurrentState)
            {
                case MovementState.Idle:
                case MovementState.Walking:
                case MovementState.Sprinting:
                    HandleLocomotion();
                    HandleRotation();
                    break;

                case MovementState.Charging:
                    playerRigidbody.linearVelocity = Vector3.zero;
                    break;

                case MovementState.Dashing:
                    HandleDashingState();
                    break;

                case MovementState.Knockback:
                    if (IsGrounded && playerRigidbody.linearVelocity.magnitude < 1f)
                    {
                        CurrentState = MovementState.Idle;
                    }
                    break;
            }
        }

        private void HandleDashingState()
        {
            dashTimer -= Time.fixedDeltaTime;

            SteerWhileDashing(); // <-- LOGIC BẺ LÁI MỚI

            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        private void SteerWhileDashing()
        {
            Vector3 steeringDirection = CalculateMoveDirection();
            if (steeringDirection.sqrMagnitude > 0.1f)
            {
                float currentDashSpeed = playerRigidbody.linearVelocity.magnitude;
                Vector3 targetVelocity = steeringDirection * currentDashSpeed;

                playerRigidbody.linearVelocity = Vector3.Lerp(
                    playerRigidbody.linearVelocity,
                    targetVelocity,
                    dashSteeringSpeed * Time.fixedDeltaTime);

                transform.rotation = Quaternion.LookRotation(playerRigidbody.linearVelocity.normalized);
            }
        }

        private void EndDash()
        {
            playerRigidbody.linearVelocity *= 0.1f;
            ApplyKnockback();
        }

        private void UpdateGroundStatus()
        {
            IsGrounded = Physics.CheckSphere(groundCheckTransform.position, groundCheckRadius, groundLayer);
        }

        private void HandleLocomotion()
        {
            Vector3 moveDirection = CalculateMoveDirection();

            if (moveDirection.sqrMagnitude < 0.01f)
            {
                CurrentState = MovementState.Idle;
                playerRigidbody.linearVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, 0);
                return;
            }

            // --- LOGIC SPRINT ĐÃ SỬA LẠI ---
            bool isTryingToSprint = playerInput.SprintInputHeld;
            float targetSpeed;

            if (isTryingToSprint && playerStamina.TryDrainStaminaForSprint(Time.fixedDeltaTime))
            {
                // Chỉ chạy nếu trừ stamina thành công
                targetSpeed = sprintSpeed;
                CurrentState = MovementState.Sprinting;
            }
            else
            {
                // Nếu không, mặc định là đi bộ
                targetSpeed = walkSpeed;
                CurrentState = MovementState.Walking;
            }

            Vector3 targetVelocity = moveDirection * targetSpeed;
            targetVelocity.y = playerRigidbody.linearVelocity.y;
            playerRigidbody.linearVelocity = targetVelocity;
        }

        private Vector3 CalculateMoveDirection()
        {
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            return (cameraForward.normalized * playerInput.MoveInput.y + cameraRight.normalized * playerInput.MoveInput.x).normalized;
        }

        private void HandleRotation()
        {
            if (CurrentState == MovementState.Sprinting || CurrentState == MovementState.Walking)
            {
                Vector3 lookDirection = CalculateMoveDirection();
                if (lookDirection == Vector3.zero) return;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void HandleDashInput()
        {
            if (playerInput.DashInputDown && CurrentState != MovementState.Charging)
            {
                CurrentState = MovementState.Charging;
                currentChargeTime = 0f;
            }

            if (playerInput.DashInputHeld && CurrentState == MovementState.Charging)
            {
                currentChargeTime += Time.deltaTime;
            }

            if (playerInput.DashInputUp && CurrentState == MovementState.Charging)
            {
                ExecuteDash();
            }
        }

        private void ExecuteDash()
        {
            DashTier selectedTier = GetDashTierForCurrentCharge();
            if (!playerStamina.TryConsumeStamina(selectedTier.staminaCost))
            {
                CurrentState = MovementState.Idle;
                return;
            }

            CurrentState = MovementState.Dashing;
            dashTimer = dashDuration;

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.AddForce(transform.forward * selectedTier.dashForce, ForceMode.Impulse);
        }

        private void ApplyKnockback()
        {
            CurrentState = MovementState.Knockback;
            playerRigidbody.AddForce(-transform.forward * knockbackForce, ForceMode.Impulse);
        }

        // --- LOGIC CHỌN NẤC DASH ĐÃ SỬA LẠI ---
        private DashTier GetDashTierForCurrentCharge()
        {
            // Duyệt ngược từ nấc mạnh nhất về nấc yếu nhất
            for (int i = dashTiers.Count - 1; i >= 0; i--)
            {
                // Nếu đủ thời gian charge cho nấc này, chọn nó ngay lập tức
                if (currentChargeTime >= dashTiers[i].chargeTimeRequired)
                {
                    return dashTiers[i];
                }
            }
            // Nếu không đủ cho bất kỳ nấc nào (trường hợp hiếm), trả về nấc yếu nhất
            return dashTiers[0];
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            }

            if (CurrentState == MovementState.Charging)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, -transform.forward * (knockbackForce / 10f));
            }
        }
    }

    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        Charging,
        Dashing,
        Knockback
    }

    [System.Serializable]
    public struct DashTier
    {
        public string tierName;
        public float chargeTimeRequired;
        public float dashForce;
        public float staminaCost;
    }
}