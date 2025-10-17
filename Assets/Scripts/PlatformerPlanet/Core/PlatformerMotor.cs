using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    [RequireComponent(typeof(CharacterController))]
    public class PlatformerMotor : MonoBehaviour
    {
        [Title("Core Components")]
        [SerializeField, Required] private CharacterController _controller;
        [SerializeField, Required] private Transform _characterVisuals;

        [Title("Configuration")]
        [SerializeField, Required, InlineEditor]
        private PlayerSettings _settings;

        private Vector3 _velocity;
        private Vector3 _moveDampVelocity;
        private float _coyoteTimeCounter;
        private bool _isFacingRight = true;
        private float _currentGravity;
        private Transform _attachedPushable;

        public bool IsGrounded { get; private set; }
        public float VerticalVelocity => _velocity.y;
        public Vector3 Velocity => _velocity;
        public bool IsFacingRight => _isFacingRight;
        public PlayerSettings Settings => _settings;
        public CharacterController Controller => _controller;

        private void Awake()
        {
            _currentGravity = _settings.Gravity;
        }

        private void Update()
        {
            PerformGroundCheck();
            HandleGravity();
            ApplyFinalMovement();
        }

        private void PerformGroundCheck()
        {
            Vector3 sphereCenter = transform.position + _controller.center + _settings.GroundCheckOffset;
            IsGrounded = Physics.CheckSphere(sphereCenter, _controller.radius + _settings.GroundCheckDistance, _settings.GroundLayer, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                _coyoteTimeCounter = _settings.CoyoteTime;
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void HandleGravity()
        {
            if (IsGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            _velocity.y += _currentGravity * Time.deltaTime;
        }

        private void ApplyFinalMovement()
        {
            if (_attachedPushable != null)
            {
                Vector3 pushableMovement = new Vector3(_velocity.x, 0, 0) * Time.deltaTime;
                _controller.Move(pushableMovement);
                _attachedPushable.position += pushableMovement;
            }
            else
            {
                _controller.Move(_velocity * Time.deltaTime);
            }
        }

        public void HandleHorizontalMovement(float horizontalInput)
        {
            float targetSpeed = horizontalInput * _settings.MoveSpeed;
            float smoothTime = IsGrounded ? _settings.MovementSmoothTime : _settings.AirControlSmoothTime;
            _velocity.x = Mathf.SmoothDamp(_velocity.x, targetSpeed, ref _moveDampVelocity.x, smoothTime);
            HandleFlip(horizontalInput);
        }

        public void HandlePushPullMovement(float horizontalInput)
        {
            float targetSpeed = horizontalInput * _settings.PushSpeed;
            _velocity.x = Mathf.SmoothDamp(_velocity.x, targetSpeed, ref _moveDampVelocity.x, _settings.MovementSmoothTime);
            HandleFlip(horizontalInput);
        }

        public void HandleSwimmingMovement(float horizontalInput, float verticalInput)
        {
            _velocity.y = Mathf.Lerp(_velocity.y, 0, _settings.SwimDamping);
            _velocity.y += _settings.SwimBuoyancy * Time.deltaTime;

            Vector3 targetVelocity = new Vector3(
                horizontalInput * _settings.SwimSpeed,
                verticalInput * _settings.SwimSpeed,
                0
            );

            _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _moveDampVelocity, _settings.SwimDamping);
            HandleFlip(horizontalInput);
        }

        public void HandleFlyingMovement(float horizontalInput, float verticalInput)
        {
            Vector3 targetVelocity = new Vector3(
                horizontalInput * _settings.FlySpeed,
                verticalInput * _settings.FlySpeed,
                0
            );

            _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _moveDampVelocity, _settings.FlyDamping);
            _velocity.y = Mathf.Clamp(_velocity.y, -_settings.FlySpeed, _settings.FlySpeed);
            HandleFlip(horizontalInput);
        }

        public void StopVerticalMovement()
        {
            _velocity.y = 0;
        }

        public void StopHorizontalMovement()
        {
            _velocity.x = 0;
            _moveDampVelocity.x = 0;
        }

        public bool PerformJump()
        {
            if (_coyoteTimeCounter > 0f)
            {
                _coyoteTimeCounter = 0f;
                _velocity.y = Mathf.Sqrt(_settings.JumpHeight * -2f * _currentGravity);
                return true;
            }
            return false;
        }

        public void Teleport()
        {
            Vector3 direction = _isFacingRight ? Vector3.right : Vector3.left;
            _controller.enabled = false;
            transform.position += direction * _settings.TeleportDistance;
            _controller.enabled = true;
            _velocity = Vector3.zero;
        }

        public void ReverseGravity()
        {
            _currentGravity *= -1;
            _characterVisuals.localScale = new Vector3(_characterVisuals.localScale.x, _characterVisuals.localScale.y * -1, _characterVisuals.localScale.z);
        }

        public void SetGravity(bool enabled)
        {
            _currentGravity = enabled ? _settings.Gravity : 0;
        }

        public void SetVelocity(Vector3 newVelocity)
        {
            _velocity = newVelocity;
        }

        public Transform FindPushableObject()
        {
            Vector3 rayOrigin = transform.position + _controller.center;
            Vector3 direction = _isFacingRight ? Vector3.right : Vector3.left;
            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _settings.WallCheckDistance, _settings.PushableLayer))
            {
                return hit.transform;
            }
            return null;
        }

        public void AttachToPushable(Transform pushable)
        {
            _attachedPushable = pushable;
        }

        public void DetachFromPushable()
        {
            _attachedPushable = null;
        }

        private void HandleFlip(float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) < 0.1f) return;
            bool shouldFaceRight = horizontalInput > 0;
            if (shouldFaceRight != _isFacingRight)
            {
                _isFacingRight = shouldFaceRight;
                _characterVisuals.localScale = new Vector3(_characterVisuals.localScale.x * -1, _characterVisuals.localScale.y, _characterVisuals.localScale.z);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 sphereCenter = transform.position + _controller.center + _settings.GroundCheckOffset;
            Gizmos.DrawWireSphere(sphereCenter, _controller.radius + _settings.GroundCheckDistance);

            Gizmos.color = Color.blue;
            Vector3 rayOrigin = transform.position + _controller.center;
            Vector3 direction = _isFacingRight ? transform.right : -transform.right;
            Gizmos.DrawLine(rayOrigin, rayOrigin + direction * _settings.WallCheckDistance);
        }
#endif
    }
}