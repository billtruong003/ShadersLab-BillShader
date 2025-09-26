using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot.itemData != null)
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
}