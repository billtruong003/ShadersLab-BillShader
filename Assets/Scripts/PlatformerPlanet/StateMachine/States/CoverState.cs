using UnityEngine;

namespace PlatformerPlanet
{
    public class CoverState : PlatformerState
    {
        public CoverState(PlatformerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Motor.StopHorizontalMovement();
            Animator.SetCover(true);
            Animator.SetCoverDirection(0); // Bắt đầu ở trạng thái nấp trung tâm
        }

        public override void Tick()
        {
            // Điều kiện thoát khỏi trạng thái Cover: Di chuyển mạnh
            if (Mathf.Abs(Input.HorizontalInput) > Motor.Settings.StillThreshold)
            {
                StateMachine.SwitchState(new GroundedState(StateMachine));
                return;
            }

            // Xử lý liếc nhìn (peek) trái/phải
            HandlePeeking();
        }

        public override void Exit()
        {
            Animator.SetCover(false);
            Animator.SetCoverDirection(0); // Reset khi thoát
        }

        private void HandlePeeking()
        {
            float horizontal = Input.HorizontalInput;
            int peekDirection = 0;

            if (horizontal > Motor.Settings.StillThreshold)
            {
                peekDirection = 1; // Peek phải
            }
            else if (horizontal < -Motor.Settings.StillThreshold)
            {
                peekDirection = -1; // Peek trái
            }

            Animator.SetCoverDirection(peekDirection);
        }
    }
}