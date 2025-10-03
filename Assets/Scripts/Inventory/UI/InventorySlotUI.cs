// Path: Assets/Scripts/Inventory/UI/InventorySlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    public int slotIndex { get; private set; }
    private InventorySystem inventorySystem;

    private Transform mainCanvasTransform;
    private static GameObject draggedIcon;

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
        bool hasItem = slot != null && slot.itemData != null;
        itemIcon.gameObject.SetActive(hasItem);

        if (hasItem)
        {
            itemIcon.sprite = slot.itemData.icon;
            itemIcon.color = Color.white;
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
    }

    public void ClearSlot()
    {
        itemIcon.gameObject.SetActive(false);
        itemIcon.sprite = null;
        quantityText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var slot = inventorySystem.GetSlotAt(slotIndex);
        if (slot != null && slot.itemData != null)
        {
            InventoryUIManager.Instance.ShowPopup(slot, slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = inventorySystem.GetSlotAt(slotIndex);
        if (slot == null || slot.itemData == null) return;

        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(mainCanvasTransform, false);
        draggedIcon.transform.SetAsLastSibling();
        var image = draggedIcon.AddComponent<Image>();
        image.sprite = itemIcon.sprite;
        image.raycastTarget = false;
        image.SetNativeSize();

        itemIcon.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
            draggedIcon = null;
        }
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