using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField, Required] private Animator _animator;

        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsPushing = Animator.StringToHash("IsPushing");
        private static readonly int IsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int IsFlying = Animator.StringToHash("IsFlying");
        private static readonly int IsInCover = Animator.StringToHash("IsInCover");
        private static readonly int CoverDirection = Animator.StringToHash("CoverDirection");

        public void SetGrounded(bool isGrounded) => _animator.SetBool(IsGrounded, isGrounded);
        public void SetPushing(bool isPushing) => _animator.SetBool(IsPushing, isPushing);
        public void SetSwimming(bool isSwimming) => _animator.SetBool(IsSwimming, isSwimming);
        public void SetFlying(bool isFlying) => _animator.SetBool(IsFlying, isFlying);
        public void UpdateMoveSpeed(float normalizedSpeed) => _animator.SetFloat(MoveSpeed, Mathf.Abs(normalizedSpeed), 0.1f, Time.deltaTime);
        public void UpdateVerticalVelocity(float velocity) => _animator.SetFloat(VerticalVelocity, velocity);
        public void PlayTargetAnimation(string stateName, float crossFadeDuration = 0.1f) => _animator.CrossFade(stateName, crossFadeDuration, 0);
        public void Trigger(string triggerName) => _animator.SetTrigger(triggerName);

        public void SetCover(bool inCover) => _animator.SetBool(IsInCover, inCover);
        public void SetCoverDirection(int direction) => _animator.SetInteger(CoverDirection, direction);
    }
}