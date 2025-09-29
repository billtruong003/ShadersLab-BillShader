// Path: Assets/Scripts/Inventory/UI/InventorySlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private InventorySystem inventorySystem; // Cần tham chiếu đến InventorySystem

    public int slotIndex { get; private set; }

    private Transform mainCanvasTransform;
    private static GameObject draggedIcon; // Dùng static để chỉ có 1 icon được kéo trên toàn bộ UI

    private void Awake()
    {
        mainCanvasTransform = GetComponentInParent<Canvas>().transform;
    }

    public void Initialize(InventorySystem system, int index)
    {
        inventorySystem = system;
        slotIndex = index;
        UpdateSlot(inventorySystem.GetSlotAt(slotIndex));
    }

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot != null && slot.itemData != null)
        {
            itemIcon.sprite = slot.itemData.icon;
            itemIcon.color = Color.white;
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        itemIcon.sprite = null;
        itemIcon.color = Color.clear;
        quantityText.text = "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = inventorySystem.GetSlotAt(slotIndex);
        if (slot == null || slot.itemData == null) return;

        // Tạo một icon tạm để kéo
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(mainCanvasTransform, false);
        draggedIcon.transform.SetAsLastSibling(); // Đảm bảo nó render trên cùng
        var image = draggedIcon.AddComponent<Image>();
        image.sprite = itemIcon.sprite;
        image.raycastTarget = false; // Để không cản trở sự kiện Drop
        image.SetNativeSize();

        // Làm mờ icon ở slot gốc
        itemIcon.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedIcon.transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
            draggedIcon = null;
        }
        // Khôi phục lại màu sắc icon nếu không có sự kiện drop hợp lệ
        UpdateSlot(inventorySystem.GetSlotAt(slotIndex));
    }

    public void OnDrop(PointerEventData eventData)
    {
        var otherSlotUI = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (otherSlotUI != null && otherSlotUI != this)
        {
            inventorySystem.SwapSlots(otherSlotUI.slotIndex, this.slotIndex);
        }
    }
}