// Path: Assets/Scripts/Enemies/EnemyHealth.cs
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private BillProgress healthBar;
    private EnemyBase enemyBase;
    private float maxHealth;
    private float currentHealth;

    public event Action<Vector3> OnDamaged;
    public event Action OnDeath;

    public bool IsDead => currentHealth <= 0;

    public void Initialize(float maxHp, EnemyBase owner)
    {
        maxHealth = maxHp;
        currentHealth = maxHealth;
        enemyBase = owner;
        UpdateHealthBar();
    }

    public void TakeDamage(float amount, Vector3 damageSource)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnDamaged?.Invoke(damageSource);
        FloatingTextManager.Instance.Show(Mathf.RoundToInt(amount).ToString(), transform.position + Vector3.up * 2f);
        UpdateHealthBar();

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    private void UpdateHealthBar()
    {
        healthBar?.SetProgress(currentHealth, maxHealth);
    }
}