// Path: Assets/Scripts/Player/PlayerHealth.cs
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private BillProgress healthBar;

    public event Action OnDeath;

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

        currentHealth -= amount;
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
        // Tạm thời chỉ disable GameObject
        // Trong tương lai sẽ gọi GameManager để xử lý màn hình thua cuộc
        Debug.Log("Player has died.");
        gameObject.SetActive(false);
    }
}