// Assets/Scripts/TuTien/Player/PlayerMovement.cs
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerCharacter))]
    public class PlayerMovement : MonoBehaviour
    {
        private enum MovementState { Grounded, Falling, Flying }

        [TabGroup("Tùy Chỉnh", "Parameters", SdfIconType.Sliders)]
        [BoxGroup("Tùy Chỉnh/Parameters/Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [BoxGroup("Tùy Chỉnh/Parameters/Movement")]
        public float rotationSpeed = 15f;

        [BoxGroup("Tùy Chỉnh/Parameters/Jumping & Flying")]
        [SerializeField] private float jumpForce = 15f;
        [BoxGroup("Tùy Chỉnh/Parameters/Jumping & Flying")]
        [Tooltip("Tốc độ di chuyển khi bay (áp dụng cho cả trục ngang và dọc).")]
        [SerializeField] private float flySpeed = 10f;
        [BoxGroup("Tùy Chỉnh/Parameters/Jumping & Flying")]
        [Tooltip("Thời gian lơ lửng sau khi nhả phím bay (giây).")]
        [SerializeField] private float flyHangTime = 2f;

        [BoxGroup("Tùy Chỉnh/Parameters/Platforming")]
        [SuffixLabel("seconds", true)]
        [SerializeField] private float dropDownDisableCollisionTime = 0.5f;

        [TabGroup("Tùy Chỉnh", "Keybindings", SdfIconType.Keyboard)]
        [BoxGroup("Tùy Chỉnh/Keybindings/Thiết Lập Phím")]
        [HideLabel]
        public Keybindings keys;

        [TabGroup("Tùy Chỉnh", "Dependencies", SdfIconType.Diagram3)]
        [BoxGroup("Tùy Chỉnh/Dependencies/Ground Check")]
        [Required, SceneObjectsOnly]
        [SerializeField] private Transform groundCheck;
        [BoxGroup("Tùy Chỉnh/Dependencies/Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.2f;
        [BoxGroup("Tùy Chỉnh/Dependencies/Ground Check")]
        [SerializeField] private LayerMask whatIsGround;

        [TabGroup("Tùy Chỉnh", "Dependencies", SdfIconType.Diagram3)]
        [BoxGroup("Tùy Chỉnh/Dependencies/Controllers")]
        [Required, SerializeField] private VoTanTuTien.VFX.PlayerVFXController vfxController;
        [Required, SerializeField] private PlayerAnimationController animationController;

        private Rigidbody rb;
        private float horizontalInput;
        private float verticalInput;
        private bool isGrounded;
        private bool canDoubleJump;
        private bool isMovingAutomatedly = false;
        private Vector3 automatedMoveTarget;
        private MovementState currentState = MovementState.Falling;
        private Coroutine endFlyStateCoroutine;
        private const string PLATFORM_LAYER_NAME = "Platform";

        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!isMovingAutomatedly)
            {
                GatherPlayerInput();
            }
            ProcessStateTransitions();
        }

        private void FixedUpdate()
        {
            PerformGroundCheck();
            ExecuteMovementBasedOnState();
        }

        private void GatherPlayerInput()
        {
            horizontalInput = Input.GetAxisRaw(keys.horizontalAxis);
            verticalInput = Input.GetAxisRaw(keys.verticalAxis);

            if (Input.GetKeyDown(keys.jumpKey)) PerformJump();
            if (Input.GetKeyDown(keys.dropDownKey)) PerformDropDown();

            if (Input.GetKeyDown(keys.flyKey)) StartFlying();
            if (Input.GetKeyUp(keys.flyKey)) StopFlyingWithDelay();
        }

        private void ProcessStateTransitions()
        {
            if (currentState == MovementState.Flying) return;

            if (isGrounded)
            {
                currentState = MovementState.Grounded;
            }
            else
            {
                currentState = MovementState.Falling;
            }
        }

        private void ExecuteMovementBasedOnState()
        {
            if (isMovingAutomatedly)
            {
                PerformAutomatedMovement();
                return;
            }

            switch (currentState)
            {
                case MovementState.Grounded:
                case MovementState.Falling:
                    PerformGroundMovement();
                    break;
                case MovementState.Flying:
                    PerformAerialMovement();
                    break;
            }
        }

        private void PerformGroundMovement()
        {
            Vector3 newVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, 0f);
            rb.linearVelocity = newVelocity;
        }

        private void PerformAerialMovement()
        {
            Vector3 moveDirection = new Vector3(horizontalInput, verticalInput, 0f).normalized;
            rb.linearVelocity = moveDirection * flySpeed;
        }

        private void PerformAutomatedMovement()
        {
            Vector3 direction = (automatedMoveTarget - transform.position).normalized;
            Vector3 moveVector = new Vector3(direction.x, 0, 0);
            rb.linearVelocity = new Vector3(moveVector.x * moveSpeed, rb.linearVelocity.y, 0f);
        }

        public void MoveTowards(Vector3 targetPosition)
        {
            automatedMoveTarget = targetPosition;
            isMovingAutomatedly = true;
        }

        public void StopMovement()
        {
            isMovingAutomatedly = false;
            horizontalInput = 0f;
            verticalInput = 0f;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        public bool IsMoving()
        {
            return Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        }

        public Vector3 GetMoveDirection()
        {
            return new Vector3(rb.linearVelocity.x, 0, 0).normalized;
        }

        private void PerformGroundCheck()
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);

            if (isGrounded && !wasGrounded)
            {
                canDoubleJump = true;
            }
        }

        private void StartFlying()
        {
            if (endFlyStateCoroutine != null)
            {
                StopCoroutine(endFlyStateCoroutine);
                endFlyStateCoroutine = null;
            }

            currentState = MovementState.Flying;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            animationController.SetFlyingState(true);
        }

        private void StopFlyingWithDelay()
        {
            if (currentState == MovementState.Flying)
            {
                endFlyStateCoroutine = StartCoroutine(EndFlyStateAfterDelay());
            }
        }

        private IEnumerator EndFlyStateAfterDelay()
        {
            yield return new WaitForSeconds(flyHangTime);
            rb.useGravity = true;
            currentState = MovementState.Falling;
            animationController.SetFlyingState(false);
            endFlyStateCoroutine = null;
        }

        private void PerformJump()
        {
            if (currentState == MovementState.Grounded)
            {
                ExecuteJump(false); // Nhảy lần đầu
                canDoubleJump = true;
            }
            else if (canDoubleJump)
            {
                ExecuteJump(true); // Nhảy lần hai
                canDoubleJump = false;
            }
        }

        private void ExecuteJump(bool isDoubleJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            vfxController.PlayJumpVFX();

            if (isDoubleJump)
            {
                animationController.TriggerDoubleJumpAnimation();
            }
            else
            {
                animationController.TriggerJumpAnimation();
            }
        }

        private void PerformDropDown()
        {
            if (isGrounded)
            {
                StartCoroutine(TemporarilyDisablePlatformCollision());
            }
        }

        private IEnumerator TemporarilyDisablePlatformCollision()
        {
            int platformLayer = LayerMask.NameToLayer(PLATFORM_LAYER_NAME);
            if (platformLayer == -1)
            {
                yield break;
            }

            Physics.IgnoreLayerCollision(gameObject.layer, platformLayer, true);
            yield return new WaitForSeconds(dropDownDisableCollisionTime);
            Physics.IgnoreLayerCollision(gameObject.layer, platformLayer, false);
        }
    }
}

namespace VoTanTuTien.Player
{
    [System.Serializable]
    public class Keybindings
    {
        [Title("Input Axes", "Tên các trục được định nghĩa trong Edit > Project Settings > Input Manager")]
        [InfoBox("Đây là tên của các trục (axis) trong hệ thống Input cũ của Unity.")]
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        [Title("Action Keys", "Các phím bấm trực tiếp cho hành động")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode dropDownKey = KeyCode.S;
        public KeyCode flyKey = KeyCode.LeftShift;

        [Title("Skill Keys")]
        public KeyCode attackKey = KeyCode.Mouse0;
        public KeyCode skill1Key = KeyCode.Alpha1;
        public KeyCode skill2Key = KeyCode.Alpha2;
        public KeyCode skill3Key = KeyCode.Alpha3;
        public KeyCode skill4Key = KeyCode.Alpha4;
    }
}