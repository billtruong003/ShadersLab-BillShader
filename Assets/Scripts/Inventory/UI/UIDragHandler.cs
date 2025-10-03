// Path: Assets/Scripts/Inventory/UI/UIDragHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Tooltip("The RectTransform of the window you want to drag. If left null, it will drag its direct parent.")]
    [SerializeField] private RectTransform targetTransform;

    private Vector2 dragOffset;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = transform.parent.GetComponent<RectTransform>();
        }
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Tính toán độ lệch giữa vị trí click chuột và pivot của cửa sổ
        // Điều này giúp cửa sổ không "nhảy" về vị trí con trỏ chuột khi bắt đầu kéo
        dragOffset = (Vector2)targetTransform.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Vị trí mới của cửa sổ là vị trí hiện tại của chuột cộng với độ lệch đã tính
        Vector2 newPosition = eventData.position + dragOffset;

        // Giới hạn vị trí của cửa sổ bên trong màn hình (tùy chọn nhưng nên có)
        newPosition = ClampToWindow(newPosition);

        targetTransform.position = newPosition;
    }

    private Vector2 ClampToWindow(Vector2 position)
    {
        Vector2 clampedPosition = position;

        Vector3[] corners = new Vector3[4];
        targetTransform.GetWorldCorners(corners);
        float width = corners[2].x - corners[0].x;
        float height = corners[1].y - corners[0].y;

        float minX = width * targetTransform.pivot.x;
        float maxX = Screen.width - (width * (1 - targetTransform.pivot.x));
        float minY = height * targetTransform.pivot.y;
        float maxY = Screen.height - (height * (1 - targetTransform.pivot.y));

        clampedPosition.x = Mathf.Clamp(position.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(position.y, minY, maxY);

        return clampedPosition;
    }
}