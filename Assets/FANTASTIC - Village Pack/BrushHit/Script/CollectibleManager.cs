using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[HideMonoScript]
public class CollectibleManager : SerializedMonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Title("Thiết lập Rendering")]
    [Required][SerializeField] private Material sharedCollectibleMaterial;

    [Title("Màu sắc & Hiệu ứng")]
    [SerializeField] private Color defaultColor = Color.cyan;
    [SerializeField] private Color collectedColor = Color.yellow;
    [SerializeField, Min(0)] private float colorChangeTime = 0.5f;
    [Required][SerializeField] private GameObject collectionParticlePrefab;

    [Title("Tối ưu hóa")]
    [SerializeField] private bool enableFrustumCulling = true;
    [SerializeField, Min(0)] private float cullingUpdateInterval = 0.2f;

    private readonly List<CollectibleItem> allItems = new List<CollectibleItem>();
    private Camera mainCamera;
    private Plane[] cameraFrustumPlanes;
    private float lastCullingTime;
    private int nextUniqueID = 0;

    public Material SharedMaterial => sharedCollectibleMaterial;
    public Color DefaultColor => defaultColor;
    public Color CollectedColor => collectedColor;
    public float ColorChangeTime => colorChangeTime;
    public GameObject CollectionParticlePrefab => collectionParticlePrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (enableFrustumCulling)
        {
            UpdateFrustumCulling();
        }
    }

    private void UpdateFrustumCulling()
    {
        if (Time.time < lastCullingTime + cullingUpdateInterval) return;

        lastCullingTime = Time.time;
        cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        foreach (var item in allItems)
        {
            if (item == null) continue;

            bool isVisible = GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, item.GetRendererBounds());
            if (item.gameObject.activeSelf != isVisible)
            {
                item.gameObject.SetActive(isVisible);
            }
        }
    }

    public int RegisterItemAndGetID(CollectibleItem item)
    {
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
        }
        return nextUniqueID++;
    }
}