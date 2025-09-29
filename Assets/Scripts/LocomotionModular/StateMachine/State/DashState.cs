// Path: Assets/Scripts/LocomotionModular/StateMachine/State/DashState.cs
using UnityEngine; // Phải có using UnityEngine để truy cập AfterImageController

namespace ModularTopDown.Locomotion
{
    public class DashState : LocomotionState
    {
        private float timer;
        private const AfterImageController.ActivationMode _previousMode = AfterImageController.ActivationMode.OnCommand;

        public DashState(LocomotionStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            stateMachine.SmokeTrailEffect?.SetActive(false);
            timer = stateMachine.dashDuration;
            input.ConsumeDashInput();
            animator.PlayTargetAnimation("Dash");

            if (stateMachine.AfterImageController != null)
            {
                stateMachine.AfterImageController.Mode = AfterImageController.ActivationMode.Always;
            }
        }

        public override void Tick(float deltaTime)
        {
            timer -= deltaTime;
            locomotion.HandleDash(stateMachine.dashSpeed);

            if (timer <= 0)
            {
                if (locomotion.IsGrounded())
                {
                    stateMachine.SwitchState(new GroundedState(stateMachine));
                }
                else
                {
                    stateMachine.SwitchState(new FallState(stateMachine));
                }
            }
        }

        public override void Exit()
        {
            if (stateMachine.AfterImageController != null)
            {
                stateMachine.AfterImageController.Mode = _previousMode;
            }
        }
    }
}