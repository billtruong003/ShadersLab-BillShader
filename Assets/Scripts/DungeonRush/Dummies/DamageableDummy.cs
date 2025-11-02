using DungeonRush.Core;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

namespace DungeonRush.Dummies
{
    [RequireComponent(typeof(HealthComponent))]
    public class DamageableDummy : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private float respawnTime = 5f;
        [SerializeField] private Color hitColor = Color.red;

        private HealthComponent healthComponent;
        private Renderer objectRenderer;
        private Color originalColor;
        private Coroutine hitFlashCoroutine;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            objectRenderer = GetComponentInChildren<Renderer>();
            if (objectRenderer != null)
            {
                originalColor = objectRenderer.material.color;
            }
        }

        private void OnEnable()
        {
            healthComponent.OnHealthChanged += HandleHealthChanged;
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            healthComponent.OnHealthChanged -= HandleHealthChanged;
            healthComponent.OnDeath -= HandleDeath;
        }

        private void HandleHealthChanged(float current, float max)
        {
            // if (current < max && objectRenderer != null)
            // {
            //     if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            //     hitFlashCoroutine = StartCoroutine(HitFlash());
            // }
        }

        private void HandleDeath()
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }

        private void Respawn()
        {
            gameObject.SetActive(true);
            healthComponent.Heal(healthComponent.MaxHealth);
            if (objectRenderer != null)
            {
                objectRenderer.material.color = originalColor;
            }
        }

        private IEnumerator HitFlash()
        {
            objectRenderer.material.color = hitColor;
            yield return new WaitForSeconds(0.1f);
            objectRenderer.material.color = originalColor;
        }
    }
}