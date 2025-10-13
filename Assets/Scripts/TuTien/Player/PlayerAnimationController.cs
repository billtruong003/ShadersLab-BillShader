// Assets/Scripts/TuTien/Player/PlayerAnimationController.cs
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [BoxGroup("Component References")]
        [Required, SerializeField] private Animator animator;
        [Required, SerializeField] private Rigidbody rb;
        [Required, SerializeField] private PlayerMovement playerMovement;

        [BoxGroup("Animation Parameters")]
        [Tooltip("Tốc độ làm mượt animation di chuyển.")]
        [SerializeField] private float movementAnimationSmoothSpeed = 10f;

        [BoxGroup("Animation Parameters/Vertical Velocity Mapping")]
        [Tooltip("Vận tốc bay lên tối đa sẽ được map về giá trị 1 cho Animator.")]
        [SerializeField] private float maxVerticalVelocityForAnim = 10f;

        [BoxGroup("Animation Parameters/Vertical Velocity Mapping")]
        [Tooltip("Vận tốc rơi tối đa sẽ được map về giá trị -1 cho Animator. (Nhập số âm)")]
        [SerializeField] private float minVerticalVelocityForAnim = -10f;

        private const string SPEED_PARAM = "Speed";
        private const string IS_GROUNDED_PARAM = "IsGrounded";
        private const string VERTICAL_VELOCITY_PARAM = "VerticalVelocity";
        private const string IS_FLYING_PARAM = "IsFlying";
        private const string JUMP_TRIGGER = "Jump";
        private const string DOUBLE_JUMP_TRIGGER = "DoubleJump";

        private float currentSpeedValue;

        private void Update()
        {
            UpdateMovementAnimation();
            UpdateJumpAndFallAnimation();
        }

        private void UpdateMovementAnimation()
        {
            float targetSpeed = playerMovement.IsMoving() ? 1f : 0f;
            currentSpeedValue = Mathf.Lerp(
                currentSpeedValue,
                targetSpeed,
                Time.deltaTime * movementAnimationSmoothSpeed
            );
            animator.SetFloat(SPEED_PARAM, currentSpeedValue, 0.1f, Time.deltaTime);
        }

        private void UpdateJumpAndFallAnimation()
        {
            animator.SetBool(IS_GROUNDED_PARAM, playerMovement.IsGrounded);

            float normalizedVerticalVelocity = MapVerticalVelocityToAnimatorRange(rb.linearVelocity.y);
            animator.SetFloat(VERTICAL_VELOCITY_PARAM, normalizedVerticalVelocity);
        }

        private float MapVerticalVelocityToAnimatorRange(float rawVelocity)
        {
            if (Mathf.Approximately(maxVerticalVelocityForAnim, 0) || Mathf.Approximately(minVerticalVelocityForAnim, 0))
            {
                return 0f;
            }

            if (rawVelocity >= 0)
            {
                return Mathf.InverseLerp(0, maxVerticalVelocityForAnim, rawVelocity);
            }

            return -Mathf.InverseLerp(0, minVerticalVelocityForAnim, rawVelocity);
        }

        public void TriggerAnimation(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return;
            animator.SetTrigger(triggerName);
        }

        public void TriggerJumpAnimation()
        {
            animator.SetTrigger(JUMP_TRIGGER);
        }

        public void TriggerDoubleJumpAnimation() // Thêm hàm mới
        {
            animator.SetTrigger(DOUBLE_JUMP_TRIGGER);
        }

        public void SetFlyingState(bool isFlying)
        {
            animator.SetBool(IS_FLYING_PARAM, isFlying);
        }
    }
}