// Path: Assets/Scripts/LocomotionModular/StateMachine/State/GroundedState.cs
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public class GroundedState : LocomotionState
    {
        private bool _isSmokeTrailActive = false;
        private const float MovementThreshold = 0.1f;

        public GroundedState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Tick(float deltaTime)
        {
            if (!locomotion.IsGrounded())
            {
                stateMachine.SwitchState(new FallState(stateMachine));
                return;
            }

            if (input.JumpInput)
            {
                stateMachine.SwitchState(new JumpState(stateMachine));
                return;
            }

            if (stateMachine.canDash && input.DashInput)
            {
                stateMachine.SwitchState(new DashState(stateMachine));
                return;
            }

            HandleSmokeTrail();
            locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning);
        }

        public override void Exit()
        {
            if (stateMachine.SmokeTrailEffect != null && _isSmokeTrailActive)
            {
                stateMachine.SmokeTrailEffect.SetActive(false);
                _isSmokeTrailActive = false;
            }
        }

        private void HandleSmokeTrail()
        {
            if (stateMachine.SmokeTrailEffect == null) return;

            Vector3 horizontalVelocity = new Vector3(locomotion.PlayerVelocity.x, 0, locomotion.PlayerVelocity.z);
            bool isMoving = horizontalVelocity.magnitude > MovementThreshold;

            if (isMoving != _isSmokeTrailActive)
            {
                _isSmokeTrailActive = isMoving;
                stateMachine.SmokeTrailEffect.SetActive(_isSmokeTrailActive);
            }
        }
    }
}