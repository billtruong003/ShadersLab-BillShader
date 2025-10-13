// Assets/Scripts/TuTien/Enemies/DummyEnemy.cs
using UnityEngine;
using VoTanTuTien.Core;
using VoTanTuTien.Interfaces;
using System.Collections;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider))]
public class DummyEnemy : MonoBehaviour, IAttackable
{
    [Title("Core Configuration")]
    [Required]
    [SerializeField] private CharacterStats statsTemplate;
    [Required]
    [SerializeField] private BillProgress healthBar;

    [Title("Rewards Pool")]
    [SerializeField] private long totalLinhLucReward = 1000;
    [SerializeField] private long totalLinhNangReward = 50;

    private CharacterStats statsInstance;
    private bool isDead = false;

    private double remainingLinhLucReward;
    private double remainingLinhNangReward;
    private double linhLucPerHealthPoint;
    private double linhNangPerHealthPoint;

    private void Awake()
    {
        InitializeStatsAndRewards();
    }

    private void OnDestroy()
    {
        if (statsInstance != null)
        {
            statsInstance.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void InitializeStatsAndRewards()
    {
        statsInstance = Instantiate(statsTemplate);
        statsInstance.InitializeRuntimeValues();
        statsInstance.OnHealthChanged += HandleHealthChanged;

        remainingLinhLucReward = totalLinhLucReward;
        remainingLinhNangReward = totalLinhNangReward;

        if (statsInstance.maxHealth > 0)
        {
            linhLucPerHealthPoint = (double)totalLinhLucReward / statsInstance.maxHealth;
            linhNangPerHealthPoint = (double)totalLinhNangReward / statsInstance.maxHealth;
        }

        UpdateHealthVisuals();
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateHealthVisuals();
        if (!isDead && currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        GetComponent<Collider>().enabled = false;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        Vector3 originalScale = transform.localScale;
        float duration = 1.0f;
        float timer = 0;

        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void UpdateHealthVisuals()
    {
        if (healthBar == null) return;
        healthBar.gameObject.SetActive(!isDead);
        healthBar.SetProgress(statsInstance.currentHealth, statsInstance.maxHealth);
    }

    #region Interface Implementations

    public Transform GetTransform() => this.transform;
    public CharacterStats GetStats() => statsInstance;
    public bool IsDead() => isDead;

    public void ReceiveDamage(float damageAmount, IRewardRecipient source)
    {
        if (isDead) return;

        float healthBeforeDamage = statsInstance.currentHealth;
        statsInstance.TakeDamage(damageAmount);
        float actualDamageDealt = healthBeforeDamage - statsInstance.currentHealth;

        if (actualDamageDealt > 0)
        {
            DistributeRewards(actualDamageDealt, source);
        }
    }

    #endregion

    private void DistributeRewards(float damageDealt, IRewardRecipient source)
    {
        long linhLucToGrant = (long)(damageDealt * linhLucPerHealthPoint);
        long linhNangToGrant = (long)(damageDealt * linhNangPerHealthPoint);

        linhLucToGrant = (long)Mathf.Min(linhLucToGrant, (long)remainingLinhLucReward);
        linhNangToGrant = (long)Mathf.Min(linhNangToGrant, (long)remainingLinhNangReward);

        if (linhLucToGrant > 0 || linhNangToGrant > 0)
        {
            remainingLinhLucReward -= linhLucToGrant;
            remainingLinhNangReward -= linhNangToGrant;
            source.ReceiveRewards(linhLucToGrant, linhNangToGrant);
        }
    }
}