// Path: Assets/Scripts/Enemies/Types/EnemyRanged.cs
using UnityEngine;

public class EnemyRanged : EnemyBase
{
    protected override void UpdateAttackingState()
    {
        if (playerTarget == null)
        {
            SwitchState(EnemyState.Idle);
            return;
        }

        // --- LOGIC SỬA LỖI & CẢI THIỆN ---
        // 1. Kiểm tra điều kiện thoát: Nếu người chơi chạy quá xa, quay lại đuổi theo/thả diều.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > enemyData.attackRange)
        {
            SwitchState(EnemyState.Chasing);
            return;
        }

        // Kiểm tra nếu người chơi lại quá gần, quay lại thả diều
        if (ShouldKite())
        {
            SwitchState(EnemyState.Kiting);
            return;
        }

        // 2. Luôn xoay về phía mục tiêu.
        FaceTarget();

        // 3. Nếu chiêu đã hồi, thì bắn.
        if (IsReadyToAttack())
        {
            InitiateAttack();
        }
        // 4. Nếu chưa, đứng yên chờ.
    }

    private void InitiateAttack()
    {
        vatAnimator.CrossFade(enemyData.animAttack, 0.1f);
        attackCooldownTimer = enemyData.attackCooldown;

        // Lên lịch bắn sau một khoảng trễ nhỏ để khớp với animation
        Invoke(nameof(FireProjectile), 0.3f);
    }

    private void FireProjectile()
    {
        if (playerTarget == null || currentState != EnemyState.Attacking) return;

        if (enemyData.projectilePrefab != null)
        {
            Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
            GameObject projInstance = ObjectPoolManager.Instance.Spawn(enemyData.projectilePrefab, spawnPos, Quaternion.identity);

            Vector3 directionToPlayer = (playerTarget.position - spawnPos).normalized;
            projInstance.GetComponent<EnemyProjectile>()?.Initialize(enemyData.attackDamage, 15f, directionToPlayer);
        }
    }
}