using UnityEngine;
using System.Collections.Generic;
using System;
using MonsterLegion.Data;

namespace MonsterLegion.Combat
{
    public class MonsterController : MonoBehaviour
    {
        public event Action<MonsterController> OnDeath;

        private MonsterInstance _instanceData;
        private MonsterSpeciesSO _speciesData;
        private List<MonsterController> _allies;
        private List<MonsterController> _opponents;
        private RuntimeStats _runtimeStats;
        private int _currentHealth;
        private float _attackCooldown;
        private MonsterController _currentTarget;
        private Transform _projectileMuzzlePoint;

        public void Initialize(MonsterInstance instance, MonsterSpeciesSO species, List<MonsterController> allies, List<MonsterController> opponents)
        {
            _instanceData = instance;
            _speciesData = species;
            _allies = allies;
            _opponents = opponents;

            _runtimeStats = instance.CalculateRuntimeStats(species);
            _currentHealth = _runtimeStats.MaxHealth;

            _projectileMuzzlePoint = FindChildTransformRecursive(transform, species.ProjectileMuzzlePointTransformName);
            if (_projectileMuzzlePoint == null)
            {
                _projectileMuzzlePoint = transform;
            }

            gameObject.name = $"{species.DisplayName} (Lv.{instance.CurrentLevel})";
        }

        private void Update()
        {
            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0)
            {
                return;
            }

            if (_currentTarget == null || !_currentTarget.IsAlive())
            {
                FindTarget();
            }

            if (_currentTarget != null)
            {
                PerformAttack();
            }
        }

        public void TakeDamage(int amount)
        {
            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public RuntimeStats GetRuntimeStats() => _runtimeStats;
        public bool IsAlive() => _currentHealth > 0;

        private void FindTarget()
        {
            float minDistance = float.MaxValue;
            MonsterController potentialTarget = null;

            foreach (var enemy in _opponents)
            {
                if (enemy == null || !enemy.IsAlive()) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    potentialTarget = enemy;
                }
            }

            _currentTarget = (potentialTarget != null && minDistance <= _speciesData.AttackRange) ? potentialTarget : null;
        }

        private void PerformAttack()
        {
            ProjectileManager.Instance.SpawnProjectile(this, _currentTarget, _speciesData.ProjectileReference, _projectileMuzzlePoint.position);
            _attackCooldown = 1f / _speciesData.AttackSpeed;
        }

        private void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        private Transform FindChildTransformRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;

                Transform result = FindChildTransformRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}