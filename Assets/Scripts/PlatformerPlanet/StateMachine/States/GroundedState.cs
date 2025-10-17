using UnityEngine;

namespace PlatformerPlanet
{
    public class GroundedState : PlatformerState
    {
        public GroundedState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Tick()
        {
            if (StateMachine.IsInCoverZone() && Mathf.Abs(Motor.Velocity.x) < Motor.Settings.StillThreshold)
            {
                StateMachine.SwitchState(new CoverState(StateMachine));
                return;
            }

            if (Input.IsJumpPressed)
            {
                StateMachine.SwitchState(new JumpState(StateMachine));
                return;
            }

            if (!Motor.IsGrounded)
            {
                StateMachine.SwitchState(new FallState(StateMachine));
                return;
            }

            if (Input.IsInteractPressed)
            {
                Transform pushable = Motor.FindPushableObject();
                if (pushable != null)
                {
                    Input.ConsumeInteractInput();
                    StateMachine.SwitchState(new PushPullState(StateMachine, pushable));
                    return;
                }
            }

            Motor.HandleHorizontalMovement(Input.HorizontalInput);
            Animator.UpdateMoveSpeed(Input.HorizontalInput);
        }
    }
}