// File: SystemEvents.cs
using System;
using UnityEngine;

public static class SystemEvents
{
    // Yêu cầu bắt đầu đặt một vật thể
    public static event Action<PlaceableItemData> OnPlacementRequested;
    public static void RaisePlacementRequested(PlaceableItemData data) => OnPlacementRequested?.Invoke(data);

    // Vật thể đã được đặt thành công
    public static event Action<PlaceableObject, Vector2Int> OnObjectPlaced;
    public static void RaiseObjectPlaced(PlaceableObject obj, Vector2Int position) => OnObjectPlaced?.Invoke(obj, position);

    // Hành động đặt bị hủy
    public static event Action OnPlacementCancelled;
    public static void RaisePlacementCancelled() => OnPlacementCancelled?.Invoke();
}

