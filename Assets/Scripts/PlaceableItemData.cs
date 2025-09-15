// File: PlaceableItemData.cs (Nâng cấp)
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewPlaceableItem", menuName = "Grid System/Placeable Item Data")]
public class PlaceableItemData : ScriptableObject
{
    public string id;
    public Vector2Int size = Vector2Int.one;
    public AssetReferenceGameObject prefabReference; // Dùng Addressables
    public Material previewMaterial;
}

