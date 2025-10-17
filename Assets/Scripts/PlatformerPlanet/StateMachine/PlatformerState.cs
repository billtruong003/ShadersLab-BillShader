namespace PlatformerPlanet
{
    public abstract class PlatformerState
    {
        protected readonly PlatformerStateMachine StateMachine;
        protected readonly IPlatformerInput Input;
        protected readonly PlatformerMotor Motor;
        protected readonly PlayerAnimator Animator;

        protected PlatformerState(PlatformerStateMachine stateMachine)
        {
            StateMachine = stateMachine;
            Input = stateMachine.Input;
            Motor = stateMachine.Motor;
            Animator = stateMachine.PlayerAnimator;
        }

        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void Exit() { }
    }
}