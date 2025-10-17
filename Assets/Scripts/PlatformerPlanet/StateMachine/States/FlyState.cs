namespace PlatformerPlanet
{
    public class FlyState : PlatformerState
    {
        public FlyState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Motor.SetGravity(false);
            Motor.StopVerticalMovement();
            Animator.SetFlying(true);
        }

        public override void Tick()
        {
            if (Input.IsFlyPressed)
            {
                Input.ConsumeFlyInput();
                StateMachine.RequestPreviousState();
                return;
            }

            Motor.HandleFlyingMovement(Input.HorizontalInput, Input.VerticalInput);
        }

        public override void Exit()
        {
            Motor.SetGravity(true);
            Animator.SetFlying(false);
        }
    }
}