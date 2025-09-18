using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Placeable_", menuName = "Placement System/Placeable Object Data")]
public class PlaceableObjectDataSO : ScriptableObject
{
    [Title("IDENTITY", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("General Info")]
    [Required("Object Name cannot be empty.")]
    public string Name = "New Object";

    [BoxGroup("General Info")]
    [Tooltip("Size of the object on the grid.")]
    public Vector2Int Size = Vector2Int.one;

    [BoxGroup("General Info")]
    [Tooltip("Vertical offset to apply when placing. Use this to correct pivot point differences.")]
    public float PlacementYOffset = 0f;

    [Title("ASSET REFERENCES", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("Asset Setup")]
    [Required]
    [AssetsOnly]
    [ValidateInput("HasPlaceableObjectComponent", "The assigned Prefab must have a 'PlaceableObject' component.")]
    [PreviewField(ObjectFieldAlignment.Left, Height = 75)]
    public GameObject PrefabToPlace;

    [BoxGroup("Asset Setup")]
    [Required]
    [AssetsOnly]
    [ValidateInput("HasPlacementPreviewComponent", "The assigned Preview Prefab must have a 'PlacementPreview' component.")]
    [PreviewField(ObjectFieldAlignment.Left, Height = 75)]
    public GameObject PreviewPrefab;

#if UNITY_EDITOR
    private bool HasPlaceableObjectComponent(GameObject prefab) => prefab != null && prefab.GetComponent<PlaceableObject>() != null;
    private bool HasPlacementPreviewComponent(GameObject prefab) => prefab != null && prefab.GetComponent<PlacementPreview>() != null;
#endif
}