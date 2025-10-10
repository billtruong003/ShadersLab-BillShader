using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
[RequireComponent(typeof(Collider), typeof(MeshRenderer))]
public class CollectibleItem : SerializedMonoBehaviour
{
    [Title("Thiết lập")]
    [SerializeField] private int pointValue = 10;

    [ShowInInspector, ReadOnly] private int uniqueID = -1;
    [ShowInInspector, ReadOnly] private bool hasBeenCollected = false;

    private MaterialPropertyBlock propertyBlock;
    private MeshRenderer meshRenderer;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int InteractionColorID = Shader.PropertyToID("_InteractionColor");
    private static readonly int InteractionProgressID = Shader.PropertyToID("_InteractionProgress");
    private static readonly int UniqueIDShaderID = Shader.PropertyToID("_UniqueID");

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        RegisterWithManager();
        InitializeShaderProperties();
    }

    private void InitializeComponents()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void RegisterWithManager()
    {
        if (CollectibleManager.Instance == null)
        {
            Debug.LogError("CollectibleManager không tồn tại trong scene.", this);
            enabled = false;
            return;
        }
        uniqueID = CollectibleManager.Instance.RegisterItemAndGetID(this);
        meshRenderer.sharedMaterial = CollectibleManager.Instance.SharedMaterial;
    }

    private void InitializeShaderProperties()
    {
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorID, CollectibleManager.Instance.DefaultColor);
        propertyBlock.SetColor(InteractionColorID, CollectibleManager.Instance.CollectedColor);
        propertyBlock.SetFloat(InteractionProgressID, 0f);
        propertyBlock.SetFloat(UniqueIDShaderID, (float)uniqueID);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenCollected || !IsPlayerInteractor(other)) return;
        Collect();
    }

    private bool IsPlayerInteractor(Collider other)
    {
        return other.TryGetComponent<BoxInteractor>(out _);
    }

    private void Collect()
    {
        hasBeenCollected = true;
        ScoreManager.Instance?.AddScore(pointValue);
        CollectibleManager.Instance?.NotifyItemCollected();
        SpawnCollectionEffects();
        AnimateVisualChange();
    }

    private void SpawnCollectionEffects()
    {
        GameObject particlePrefab = CollectibleManager.Instance.CollectionParticlePrefab;
        if (particlePrefab != null && ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Spawn(particlePrefab, transform.position, Quaternion.identity);
        }
    }

    private void AnimateVisualChange()
    {
        float animationTime = CollectibleManager.Instance.ColorChangeTime;
        LeanTween.value(gameObject, 0f, 1f, animationTime)
            .setOnUpdate(UpdateInteractionProgress)
            .setEase(LeanTweenType.easeOutQuad);
    }

    private void UpdateInteractionProgress(float progress)
    {
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(InteractionProgressID, progress);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public Bounds GetRendererBounds()
    {
        return meshRenderer.bounds;
    }
}