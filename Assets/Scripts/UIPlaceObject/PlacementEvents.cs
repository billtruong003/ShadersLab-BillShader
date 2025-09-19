using System;

public static class PlacementEvents
{
    // UI -> System: Yêu cầu bắt đầu quá trình đặt một đối tượng
    public static Action<PlaceableObjectDataSO, DraggableUIItem> OnRequestPlacement;

    // UI -> System: Yêu cầu kết thúc quá trình kéo thả (để quyết định đặt hoặc hủy)
    public static Action OnDragEnd;

    // System -> UI: Thông báo một đối tượng đã được đặt thành công
    public static Action<PlaceableObjectDataSO, PlaceableObject> OnPlacementSucceeded;

    // System -> UI: Thông báo quá trình đặt đã bị hủy hoặc thất bại
    public static Action<PlaceableObjectDataSO> OnPlacementFailed;

    // UI -> System: Yêu cầu vào chế độ xóa
    public static Action OnEnterRemovalMode;

    // System -> All: Thông báo thoát khỏi mọi chế độ đặc biệt (đặt/xóa)
    public static Action OnModeExited;

    // System -> UI: Thông báo một đối tượng đã bị xóa
    public static Action<PlaceableObjectDataSO> OnObjectRemoved;
}