namespace PlatformerPlanet
{
    public class SwimState : PlatformerState
    {
        public SwimState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Motor.SetGravity(false);
            Motor.StopVerticalMovement();
            Animator.SetSwimming(true);
        }

        public override void Tick()
        {
            Motor.HandleSwimmingMovement(Input.HorizontalInput, Input.VerticalInput);
        }

        public override void Exit()
        {
            Motor.SetGravity(true);
            Animator.SetSwimming(false);
        }
    }
}