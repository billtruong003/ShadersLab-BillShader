using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionHighlight; // Một cái viền để cho biết ô đang được chọn

    public void UpdateSlot(InventorySlot slot)
    {
        bool hasItem = slot != null && slot.itemData != null;
        itemIcon.enabled = hasItem;
        quantityText.enabled = hasItem;

        if (hasItem)
        {
            itemIcon.sprite = slot.itemData.icon;
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
    }

    public void SetHighlight(bool isSelected)
    {
        selectionHighlight.SetActive(isSelected);
    }
}