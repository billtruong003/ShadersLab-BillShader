// Path: Assets/Scripts/Enemies/Types/EnemyMelee.cs
using UnityEngine;

public class EnemyMelee : EnemyBase
{
    private float attackStateTimer; // Timer cho animation

    protected override void UpdateAttackingState()
    {
        if (playerTarget == null)
        {
            SwitchState(EnemyState.Idle);
            return;
        }

        // --- LOGIC SỬA LỖI ---
        // 1. Kiểm tra điều kiện thoát: Nếu người chơi chạy quá xa, quay lại đuổi theo.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > enemyData.attackRange * 1.2f) // Thêm 1 khoảng buffer nhỏ
        {
            SwitchState(EnemyState.Chasing);
            return;
        }

        // 2. Luôn xoay về phía mục tiêu khi ở trạng thái tấn công.
        FaceTarget();

        // 3. Nếu đang trong animation tấn công, chỉ chờ nó kết thúc.
        if (attackStateTimer > 0)
        {
            attackStateTimer -= Time.deltaTime;
            return;
        }

        // 4. Nếu animation đã xong VÀ chiêu đã hồi, thì thực hiện đòn tấn công mới.
        if (IsReadyToAttack())
        {
            InitiateAttack();
        }

        // 5. Nếu chiêu CHƯA hồi, không làm gì cả. Kẻ địch sẽ đứng yên chờ đợi.
        // Đây là thay đổi quan trọng nhất để sửa lỗi.
    }

    private void InitiateAttack()
    {
        vatAnimator.CrossFade(enemyData.animAttack, 0.1f);

        attackCooldownTimer = enemyData.attackCooldown;
        attackStateTimer = enemyData.attackAnimDuration;

        Invoke(nameof(DealDamage), 0.5f);
    }

    private void DealDamage()
    {
        if (playerTarget != null && currentState == EnemyState.Attacking)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= enemyData.attackRange)
            {
                playerTarget.GetComponent<PlayerHealth>()?.TakeDamage(enemyData.attackDamage);
            }
        }
    }
}