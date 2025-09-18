using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class PlacementPreview : MonoBehaviour
{
    [Title("VISUAL FEEDBACK")]
    [Required][SerializeField] private Material validPlacementMaterial;
    [Required][SerializeField] private Material invalidPlacementMaterial;
    [ValidateInput("IsRendererListPopulated")][SerializeField] private List<Renderer> renderers;

    public bool IsPlacementValid { get; private set; }

    private PlaceableObjectDataSO objectData;

    public void Initialize(PlaceableObjectDataSO data) => objectData = data;
    public PlaceableObjectDataSO GetObjectData() => objectData;

    public void UpdateVisuals(bool isValid)
    {
        IsPlacementValid = isValid;
        Material materialToApply = IsPlacementValid ? validPlacementMaterial : invalidPlacementMaterial;
        foreach (var rend in renderers)
        {
            // Sử dụng sharedMaterial để tránh tạo instance material mới trong Editor
            if (rend.sharedMaterial != materialToApply)
            {
                rend.material = materialToApply;
            }
        }
    }

    public void SetVisibility(bool isVisible)
    {
        // Kiểm tra để tránh gọi SetActive không cần thiết
        if (gameObject.activeSelf != isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }

#if UNITY_EDITOR
    private bool IsRendererListPopulated(List<Renderer> list) => list != null && list.Count > 0 && list.All(r => r != null);
#endif
}