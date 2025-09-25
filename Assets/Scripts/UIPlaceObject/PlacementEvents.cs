using System;

public static class PlacementEvents
{
    // Yêu cầu từ UI -> System
    public static Action<PlaceableObjectDataSO, DraggableUIItem> OnRequestPlacement;
    public static Action OnDragEnd;
    public static Action OnEnterRemovalMode;
    public static Action<PlaceableObject> OnRequestObjectRemoval; // SỰ KIỆN MỚI

    // Thông báo từ System -> Toàn bộ
    public static Action OnModeExited;
    public static Action<DraggableUIItem, PlaceableObject> OnPlacementSucceeded;
    public static Action<DraggableUIItem> OnPlacementFailed;
    public static Action<PlaceableObject> OnObjectRemoved; // Sự kiện này giờ chỉ mang ý nghĩa "thông báo đã xóa"
}