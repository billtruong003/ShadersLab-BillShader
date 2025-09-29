// Path: Assets/Scripts/Enemies/DummyHealth.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class DummyHealth : MonoBehaviour
{
    // Hằng số cho tên thuộc tính shader, dễ dàng thay đổi ở một nơi
    private const string SHADER_COLOR_PROPERTY = "_BaseColor"; // URP/HDRP dùng "_BaseColor", Built-in RP thường dùng "_Color"

    [Header("Health & UI")]
    [SerializeField] private float maxHealth = 500f;
    [SerializeField] private BillProgress healthBar; // Kéo thanh máu của Dummy vào đây

    [Header("Regeneration")]
    [SerializeField] private bool canRegenerate = true;
    [SerializeField] private float regenerationRate = 10f;
    [SerializeField] private float regenerationDelay = 3f;

    [Header("Effects")]
    [Tooltip("Renderer chính của Dummy để áp dụng hiệu ứng.")]
    [SerializeField] private Renderer dummyRenderer;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.1f;

    private float currentHealth;
    private float lastDamageTime;
    private Rigidbody _rigidbody;
    private Color originalColor;

    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyID;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = false;

        propertyBlock = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID(SHADER_COLOR_PROPERTY);

        // Lấy màu gốc từ thuộc tính của material
        if (dummyRenderer != null)
        {
            originalColor = dummyRenderer.sharedMaterial.GetColor(colorPropertyID);
        }

        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (canRegenerate && Time.time - lastDamageTime > regenerationDelay)
        {
            Heal(regenerationRate * Time.deltaTime);
        }
    }

    public void TakeDamage(float damageAmount, Vector3 damageSource)
    {
        currentHealth -= damageAmount;
        lastDamageTime = Time.time;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        FloatingTextManager.Instance.Show(Mathf.RoundToInt(damageAmount).ToString(), transform.position + Vector3.up * 1.5f);

        ApplyKnockback(damageSource);
        TriggerDamageFlash();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Heal(float amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBar?.SetProgress(currentHealth, maxHealth);
    }

    private void ApplyKnockback(Vector3 damageSource)
    {
        _rigidbody.linearVelocity = Vector3.zero;
        Vector3 knockbackDirection = (transform.position - damageSource).normalized;
        knockbackDirection.y = 0;
        _rigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
    }

    private void TriggerDamageFlash()
    {
        if (dummyRenderer == null) return;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        // Ghi đè màu bằng Property Block
        dummyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyID, damageFlashColor);
        dummyRenderer.SetPropertyBlock(propertyBlock);

        yield return new WaitForSeconds(damageFlashDuration);

        // Trả lại màu gốc
        dummyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyID, originalColor);
        dummyRenderer.SetPropertyBlock(propertyBlock);
    }

    private void Die()
    {
        Debug.Log("Dummy 'died'. Resetting health.");
        currentHealth = maxHealth;
        UpdateHealthBar();
    }
}