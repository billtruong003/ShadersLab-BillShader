namespace PlatformerPlanet
{
    public class JumpState : PlatformerState
    {
        public JumpState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Input.ConsumeJumpInput();
            if (Motor.PerformJump())
            {
                Animator.PlayTargetAnimation("Jump");
            }
            else
            {
                // Failed to jump (e.g., coyote time expired), go straight to falling
                StateMachine.SwitchState(new FallState(StateMachine));
            }
        }

        public override void Tick()
        {
            Motor.HandleHorizontalMovement(Input.HorizontalInput);

            // Transition to FallState when downward velocity is detected
            if (Motor.VerticalVelocity < 0f)
            {
                StateMachine.SwitchState(new FallState(StateMachine));
            }
        }
    }
}