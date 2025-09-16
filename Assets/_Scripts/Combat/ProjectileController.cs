using UnityEngine;
using MonsterLegion.Data;

namespace MonsterLegion.Combat
{
    public class ProjectileController : MonoBehaviour
    {
        private MonsterController target;
        private ProjectileSO projectileData;
        private RuntimeStats attackerStats;

        private float speed;

        public void Launch(MonsterController attacker, MonsterController target, ProjectileSO data)
        {
            this.attackerStats = attacker.GetRuntimeStats();
            this.target = target;
            this.projectileData = data;
            this.speed = data.ProjectileSpeed;

            // Kích hoạt lại GameObject nếu nó đang ở trong pool
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                // Mục tiêu đã chết hoặc không tồn tại, tự hủy
                gameObject.SetActive(false);
                return;
            }

            MoveTowardsTarget();
        }

        private void MoveTowardsTarget()
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

            // Kiểm tra va chạm bằng khoảng cách
            if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            {
                OnImpact();
            }
        }

        private void OnImpact()
        {
            // Gây sát thương cho mục tiêu
            int damage = DamageCalculator.Calculate(attackerStats, target.GetRuntimeStats(), projectileData);
            target.TakeDamage(damage);

            // TODO: Tạo hiệu ứng va chạm (VFX) tại đây

            // Quay trở lại pool
            gameObject.SetActive(false);
        }
    }
}