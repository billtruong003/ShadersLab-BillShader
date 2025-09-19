using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    // Lớp này chỉ chịu trách nhiệm đọc input và thông báo cho hệ thống
    // mà không cần biết hệ thống sẽ làm gì với thông tin đó.

    void Update()
    {
        // Có thể mở rộng để sử dụng Input System mới của Unity
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            // Các hệ thống khác sẽ lắng nghe sự kiện này
            // và quyết định hành động dựa trên trạng thái hiện tại của chúng.
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Luôn luôn là hành động hủy/quay lại
            PlacementEvents.OnModeExited?.Invoke();
        }
    }

    private bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();
}