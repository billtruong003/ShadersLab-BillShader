using UnityEngine;

namespace PlatformerPlanet
{
    public class PushPullState : PlatformerState
    {
        private Transform _pushableObject;

        public PushPullState(PlatformerStateMachine stateMachine, Transform pushable) : base(stateMachine)
        {
            _pushableObject = pushable;
        }

        public override void Enter()
        {
            Motor.AttachToPushable(_pushableObject);
            Animator.SetPushing(true);
        }

        public override void Tick()
        {
            if (!Input.IsInteractPressed || Motor.FindPushableObject() == null)
            {
                StateMachine.SwitchState(new GroundedState(StateMachine));
                return;
            }

            Motor.HandlePushPullMovement(Input.HorizontalInput);
            Animator.UpdateMoveSpeed(Input.HorizontalInput);
        }

        public override void Exit()
        {
            Motor.DetachFromPushable();
            Animator.SetPushing(false);
        }
    }
}