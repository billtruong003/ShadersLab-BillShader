using Sirenix.OdinInspector;
using UnityEngine;

namespace DungeonRush
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator playerAnimator;

        private const string animSpeedParam = "Speed";
        private const string animAttackTriggerParam = "Attack";
        private const string animAttackIDParam = "AttackID";
        private const string animDashTriggerParam = "Dash";

        [ShowInInspector, ReadOnly, InfoBox(animSpeedParam)] private static readonly int SpeedParam = Animator.StringToHash(animSpeedParam);
        [ShowInInspector, ReadOnly, InfoBox("Attack")] private static readonly int AttackTriggerParam = Animator.StringToHash(animAttackTriggerParam);
        [ShowInInspector, ReadOnly, InfoBox("AttackID")] private static readonly int AttackIDParam = Animator.StringToHash(animAttackIDParam);
        [ShowInInspector, ReadOnly, InfoBox("Dash")] private static readonly int DashTriggerParam = Animator.StringToHash(animDashTriggerParam);

        private void Awake()
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        public void UpdateMovementAnimation(float normalizedSpeed)
        {
            playerAnimator.SetFloat(SpeedParam, normalizedSpeed, 0.1f, Time.deltaTime);
        }

        public void TriggerAttack(int attackID)
        {
            playerAnimator.SetFloat(AttackIDParam, attackID);
            playerAnimator.SetTrigger(AttackTriggerParam);
        }

        public void TriggerDash()
        {
            playerAnimator.SetTrigger(DashTriggerParam);
        }

        public void ResetCombatAnimation()
        {
            playerAnimator.SetFloat(AttackIDParam, 0);
        }
    }
}