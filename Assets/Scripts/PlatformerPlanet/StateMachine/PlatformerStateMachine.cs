using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    [RequireComponent(typeof(IPlatformerInput), typeof(PlatformerMotor))]
    public class PlatformerStateMachine : MonoBehaviour
    {
        [field: Title("Core Components")]
        [field: SerializeField, Required] public PlatformerMotor Motor { get; private set; }
        [field: SerializeField, Required] public PlayerAnimator PlayerAnimator { get; private set; }
        [field: SerializeField, Required] public LedgeDetector LedgeDetector { get; private set; }

        public IPlatformerInput Input { get; private set; }
        private PlatformerState _currentState;
        private PlatformerState _previousState;
        private bool _isInWater = false;
        private bool _isInCoverZone = false;

        private void Awake()
        {
            Input = GetComponent<IPlatformerInput>();
        }

        private void Start()
        {
            SwitchState(new GroundedState(this));
        }

        private void Update()
        {
            _currentState?.Tick();
            HandleGlobalAbilities();
            UpdateAnimatorParameters();
        }

        public void SwitchState(PlatformerState newState)
        {
            _currentState?.Exit();
            _previousState = _currentState;
            _currentState = newState;
            _currentState?.Enter();
        }

        public void RequestPreviousState()
        {
            if (_previousState != null)
            {
                SwitchState(_previousState);
            }
            else
            {
                SwitchState(new GroundedState(this));
            }
        }

        private void HandleGlobalAbilities()
        {
            if (Input.IsTeleportPressed) { Motor.Teleport(); Input.ConsumeTeleportInput(); }
            if (Input.IsReverseGravityPressed) { Motor.ReverseGravity(); Input.ConsumeReverseGravityInput(); }
            if (Input.IsFlyPressed) { SwitchState(new FlyState(this)); Input.ConsumeFlyInput(); }
        }

        private void UpdateAnimatorParameters()
        {
            PlayerAnimator.SetGrounded(Motor.IsGrounded);
            PlayerAnimator.UpdateVerticalVelocity(Motor.VerticalVelocity);
        }

        public void EnterWater()
        {
            _isInWater = true;
            SwitchState(new SwimState(this));
        }

        public void ExitWater()
        {
            _isInWater = false;
            if (_currentState is SwimState)
            {
                SwitchState(new FallState(this));
            }
        }

        public bool IsInWater() => _isInWater;

        public bool IsInCoverZone() => _isInCoverZone;

        public void EnterCoverZone()
        {
            _isInCoverZone = true;
        }

        public void ExitCoverZone()
        {
            _isInCoverZone = false;
            if (_currentState is CoverState)
            {
                SwitchState(new GroundedState(this));
            }
        }
    }
}