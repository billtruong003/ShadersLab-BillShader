using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Nebulanook.Core;
using Shmackle.Utils.CoroutinesTimer;

namespace Nebulanook.Environment
{
    public enum ArborState
    {
        Dormant,
        Active,
        Recovering
    }

    public class AncientArborNode : MonoBehaviour, IBumpable
    {
        [Title("Arbor Identity")]
        [SerializeField] private string treeName = "The Hydrus Vine";
        [SerializeField] private Color coreColor = Color.cyan;

        [Title("Mechanics")]
        [SerializeField] private ArborState currentState = ArborState.Active;
        [SerializeField] private int maxDropsPerCycle = 3;
        [SerializeField] private float cooldownDuration = 10f;
        [SerializeField] private float forceThreshold = 100f;

        [Title("Drops")]
        [SerializeField] private GameObject fruitPrefab;
        [SerializeField] private Transform dropOrigin;
        [SerializeField] private float dropForce = 5f;

        [Title("Visuals")]
        [SerializeField] private MeshRenderer treeRenderer;
        [SerializeField] private ParticleSystem bumpEffect;
        [SerializeField] private ParticleSystem recoverEffect;
        [SerializeField] private float shakeMagnitude = 0.5f;

        private int currentCycleDrops;
        private Coroutine recoverCoroutine;
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (treeRenderer == null) treeRenderer = GetComponent<MeshRenderer>();
            UpdateVisualState();
        }

        public void OnBump(Vector3 impactDirection, float impactForce)
        {
            if (currentState != ArborState.Active) return;
            if (impactForce < forceThreshold) return;

            PerformBumpShake();
            SpawnFruit(impactDirection);

            if (bumpEffect != null) bumpEffect.Play();

            currentCycleDrops++;
            if (currentCycleDrops >= maxDropsPerCycle)
            {
                EnterRecovery();
            }
        }

        private void SpawnFruit(Vector3 impactDir)
        {
            if (fruitPrefab == null) return;

            GameObject fruit = Instantiate(fruitPrefab, dropOrigin.position, Random.rotation);
            Rigidbody rb = fruit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 dropDir = (Vector3.up + impactDir * 0.5f + Random.insideUnitSphere * 0.2f).normalized;
                rb.AddForce(dropDir * dropForce, ForceMode.Impulse);
            }
        }

        private void EnterRecovery()
        {
            currentState = ArborState.Recovering;
            UpdateVisualState();
            if (recoverCoroutine != null) StopCoroutine(recoverCoroutine);
            recoverCoroutine = StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            yield return CoroutineTimeUtils.GetWaitForSeconds(cooldownDuration);

            currentState = ArborState.Active;
            currentCycleDrops = 0;
            UpdateVisualState();

            if (recoverEffect != null) recoverEffect.Play();
        }

        private void PerformBumpShake()
        {
            StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                transform.localScale = originalScale + Random.insideUnitSphere * (shakeMagnitude * (1 - elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = originalScale;
        }

        private void UpdateVisualState()
        {
            if (treeRenderer == null) return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            treeRenderer.GetPropertyBlock(block);

            switch (currentState)
            {
                case ArborState.Active:
                    block.SetColor("_BaseColor", coreColor);
                    block.SetFloat("_Saturation", 1f);
                    break;
                case ArborState.Recovering:
                    block.SetColor("_BaseColor", Color.gray);
                    block.SetFloat("_Saturation", 0.2f);
                    break;
                case ArborState.Dormant:
                    block.SetColor("_BaseColor", Color.black);
                    break;
            }
            treeRenderer.SetPropertyBlock(block);
        }

        [Button("Force Activate")]
        public void ActivateTree()
        {
            currentState = ArborState.Active;
            UpdateVisualState();
        }
    }
}