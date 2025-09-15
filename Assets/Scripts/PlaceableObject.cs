// File: PlaceableObject.cs
using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public PlaceableItemData Data { get; private set; }
    public Vector2Int OriginCoordinate { get; private set; }

    public void Initialize(PlaceableItemData data, Vector2Int origin)
    {
        Data = data;
        OriginCoordinate = origin;
        gameObject.name = $"{data.id}_{origin.x}_{origin.y}";
    }

    public void ReturnToPool()
    {
        // Logic trả về pool sẽ được gọi từ đây
        AsyncObjectPooler.Instance.ReturnObject(Data.prefabReference.AssetGUID, gameObject);
    }
}