namespace PlatformerPlanet
{
    public class FallState : PlatformerState
    {
        public FallState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Animator.PlayTargetAnimation("Fall");
        }

        public override void Tick()
        {
            if (StateMachine.LedgeDetector.LedgeDetected && Motor.VerticalVelocity < 0f)
            {
                StateMachine.SwitchState(new ClimbState(StateMachine, StateMachine.LedgeDetector.LedgePosition));
                return;
            }

            if (Motor.IsGrounded)
            {
                StateMachine.SwitchState(new GroundedState(StateMachine));
                return;
            }

            if (Input.IsJumpPressed)
            {
                // Logic cho double jump có thể được thêm vào đây
            }

            Motor.HandleHorizontalMovement(Input.HorizontalInput);
        }
    }
}