// Assets/Scripts/TuTien/Player/Matoc/MaTocCharacter.cs
using UnityEngine;
using VoTanTuTien.Core;
using VoTanTuTien.Interfaces;
using VoTanTuTien.Skills;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VoTanTuTien.UI;

namespace VoTanTuTien.Player
{
    public class ActiveBuff
    {
        public VoTanTuTien.Core.StatModifier Modifier;
        public float RemainingTime;
    }

    [RequireComponent(typeof(PlayerMovement), typeof(PlayerAnimationController))]
    public class MaTocCharacter : PlayerCharacter
    {
        [Title("Character Components")]
        [Required, SerializeField] private PlayerMovement movementController;
        [Required, SerializeField] private PlayerAnimationController animationController;
        [Required, SerializeField] private Transform characterModel;

        [Title("Combat Settings")]
        [Required, SerializeField] private KeyCode basicAttackKey = KeyCode.Mouse0;
        [SerializeField] private float targetSearchRadius = 20f;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private Camera mainCamera;

        [Title("Skill Configuration")]
        [InfoBox("Gán các ScriptableObject SkillData vào đây. Slot 0 là đòn đánh thường.")]
        [SerializeField] private List<SkillData> skills;

        public IAttackable CurrentTarget { get; private set; }

        private readonly Dictionary<SkillData, float> skillCooldowns = new Dictionary<SkillData, float>();
        private readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
        private Coroutine currentActionCoroutine;
        private bool isPerformingAction = false;

        protected override void Awake()
        {
            base.Awake();
            InstantiateSkills();
        }

        private void Start()
        {
            Stats.OnLinhLucGained += HandleLinhLucGained;
            Stats.OnLinhNangGained += HandleLinhNangGained;
        }

        private void OnDestroy()
        {
            if (Stats != null)
            {
                Stats.OnLinhLucGained -= HandleLinhLucGained;
                Stats.OnLinhNangGained -= HandleLinhNangGained;
            }
        }

        /// <summary>
        /// Tạo các bản sao của SkillData để mỗi nhân vật có thể nâng cấp độc lập.
        /// </summary>
        private void InstantiateSkills()
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null)
                {
                    skills[i] = Instantiate(skills[i]);
                }
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateBuffs();
            UpdateModelRotation();
        }

        private void HandleInput()
        {
            if (isPerformingAction) return;

            if (Input.GetMouseButtonDown(1))
            {
                TrySelectTargetWithRaycast();
            }

            if (Input.GetKeyDown(basicAttackKey))
            {
                AttemptToUseSkill(0);
            }

            for (int i = 1; i <= 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    AttemptToUseSkill(i);
                }
            }
        }

        private void AttemptToUseSkill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count || skills[skillIndex] == null) return;

            SkillData skill = skills[skillIndex];

            if (skill.type != SkillType.SelfBuff && (CurrentTarget == null || CurrentTarget.IsDead()))
            {
                FindAndSetNearestTarget();
                if (CurrentTarget == null) return;
            }

            if (skillCooldowns.TryGetValue(skill, out float lastUsed) && Time.time < lastUsed + skill.GetCurrentCooldown()) return;
            if (!Stats.HasEnoughMana(skill.manaCost)) return;

            currentActionCoroutine = StartCoroutine(ProcessActionSequence(skill));
        }

        private IEnumerator ProcessActionSequence(SkillData skill)
        {
            isPerformingAction = true;

            yield return StartCoroutine(HandlePositioningForSkill(skill));

            PerformSkillExecution(skill);

            yield return new WaitForSeconds(0.2f);
            isPerformingAction = false;
        }

        private IEnumerator HandlePositioningForSkill(SkillData skill)
        {
            if (skill.type == SkillType.SelfBuff || CurrentTarget == null) yield break;

            Transform targetTransform = CurrentTarget.GetTransform();

            if (skill.type == SkillType.Melee)
            {
                while (Vector3.Distance(transform.position, targetTransform.position) > skill.attackRange)
                {
                    movementController.MoveTowards(targetTransform.position);
                    yield return null;
                }
                movementController.StopMovement();
            }
        }

        private void PerformSkillExecution(SkillData skill)
        {
            if (CurrentTarget != null && skill.type != SkillType.SelfBuff)
            {
                Vector3 directionToTarget = CurrentTarget.GetTransform().position - transform.position;
                directionToTarget.y = 0;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToTarget);
                }
            }

            Stats.UseMana(skill.manaCost);
            skillCooldowns[skill] = Time.time;
            animationController.TriggerAnimation(skill.animationTrigger);
            skill.Activate(this);
        }

        private void UpdateBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].RemainingTime -= Time.deltaTime;
                if (activeBuffs[i].RemainingTime <= 0)
                {
                    Stats.RemoveModifier(activeBuffs[i].Modifier);
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        public void ApplyBuff(VoTanTuTien.Core.StatModifier modifier, float duration)
        {
            Stats.AddModifier(modifier);
            activeBuffs.Add(new ActiveBuff { Modifier = modifier, RemainingTime = duration });
        }

        private void FindAndSetNearestTarget()
        {
            var colliders = Physics.OverlapSphere(transform.position, targetSearchRadius, enemyLayerMask);
            CurrentTarget = colliders
                .Select(col => col.GetComponent<IAttackable>())
                .Where(target => target != null && !target.IsDead())
                .OrderBy(target => Vector3.Distance(transform.position, target.GetTransform().position))
                .FirstOrDefault();
        }

        private void TrySelectTargetWithRaycast()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, enemyLayerMask))
            {
                if (hit.collider.TryGetComponent<IAttackable>(out IAttackable target))
                {
                    CurrentTarget = target;
                }
            }
        }

        private void UpdateModelRotation()
        {
            if (movementController.IsMoving() && !isPerformingAction)
            {
                Vector3 moveDirection = movementController.GetMoveDirection();
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, Time.deltaTime * movementController.rotationSpeed);
                }
            }
        }

        private void HandleLinhLucGained(long amount)
        {
            UI.FloatingTextManager.Instance.ShowLinhLucGain(amount, transform.position + Vector3.up * 2.2f);
        }

        private void HandleLinhNangGained(long amount)
        {
            UI.FloatingTextManager.Instance.ShowLinhNangGain(amount, transform.position + Vector3.up * 1.8f);
        }

        [Button("Nâng Cấp Skill 1", ButtonSizes.Large), FoldoutGroup("Debug Controls")]
        private void DebugUpgradeSkill1() => UpgradeSkill(1);

        public void UpgradeSkill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skills.Count || skills[skillIndex] == null) return;

            SkillData skill = skills[skillIndex];
            if (skill.IsMaxLevel())
            {
                Debug.Log($"{skill.skillName} đã đạt cấp tối đa.");
                return;
            }

            SkillUpgradeTier nextTier = skill.GetNextUpgradeTier();
            if (Stats.TrySpendLinhNang(nextTier.linhNangCost))
            {
                skill.currentLevel++;
                Debug.Log($"Đã nâng cấp {skill.skillName} lên cấp {skill.currentLevel}!");
            }
            else
            {
                Debug.Log($"Không đủ Linh Năng để nâng cấp {skill.skillName}. Cần {nextTier.linhNangCost}.");
            }
        }
    }
}