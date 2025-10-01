// Path: Assets/Scripts/Player/PlayerHealth.cs
using UnityEngine;
using System;

public class DamageEventArgs : EventArgs
{
    public float DamageAmount { get; set; }
    public bool IsBlocked { get; set; }

    public DamageEventArgs(float damage)
    {
        DamageAmount = damage;
        IsBlocked = false;
    }
}

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private BillProgress healthBar;

    public event Action OnDeath;
    public event EventHandler<DamageEventArgs> OnBeforeDamageTaken;

    private float currentHealth;
    public bool IsDead => currentHealth <= 0;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        var damageArgs = new DamageEventArgs(amount);
        OnBeforeDamageTaken?.Invoke(this, damageArgs);

        if (damageArgs.IsBlocked)
        {
            return;
        }

        float finalDamage = damageArgs.DamageAmount;
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();

        if (IsDead)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBar?.SetProgress(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Debug.Log("Player has died.");
        gameObject.SetActive(false);
    }
}