using UnityEngine;
using System.Collections;

namespace PlatformerPlanet
{
    public class ClimbState : PlatformerState
    {
        private readonly Vector3 _ledgePosition;
        private readonly float _climbDuration = 0.8f;
        private Coroutine _climbCoroutine;

        public ClimbState(PlatformerStateMachine stateMachine, Vector3 ledgePosition) : base(stateMachine)
        {
            _ledgePosition = ledgePosition;
        }

        public override void Enter()
        {
            Motor.Controller.enabled = false;
            Motor.SetGravity(false);
            Motor.SetVelocity(Vector3.zero);
            Animator.PlayTargetAnimation("Climb");

            _climbCoroutine = StateMachine.StartCoroutine(ClimbSequence());
        }

        private IEnumerator ClimbSequence()
        {
            Vector3 startPos = StateMachine.transform.position;
            float endYPos = _ledgePosition.y;
            float endXPos = _ledgePosition.x + (Motor.IsFacingRight ? -Motor.Controller.radius : Motor.Controller.radius);
            Vector3 endPos = new Vector3(endXPos, endYPos, 0);

            float elapsedTime = 0f;
            while (elapsedTime < _climbDuration)
            {
                StateMachine.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / _climbDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            StateMachine.transform.position = endPos;
            StateMachine.SwitchState(new GroundedState(StateMachine));
        }

        public override void Exit()
        {
            if (_climbCoroutine != null)
            {
                StateMachine.StopCoroutine(_climbCoroutine);
            }
            Motor.Controller.enabled = true;
            Motor.SetGravity(true);
        }
    }
}