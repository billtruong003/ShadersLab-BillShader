using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private TextMeshProUGUI slotNameText;
    public EquipmentSlotType slotType;

    private Button button;
    private EquipmentSystem equipmentSystem;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnSlotClicked);
        if (slotNameText != null)
        {
            slotNameText.text = slotType.ToString();
        }
    }

    public void Initialize(EquipmentSystem eqSystem)
    {
        equipmentSystem = eqSystem;
        UpdateSlotVisual(equipmentSystem.GetEquippedItem(slotType));
    }

    public void UpdateSlotVisual(EquipmentItemData item)
    {
        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = defaultIcon;
            iconImage.color = new Color(1, 1, 1, 0.5f);
        }
    }

    private void OnSlotClicked()
    {
        equipmentSystem?.Unequip(slotType);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var sourceSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (sourceSlotUI == null) return;

        var inventorySystem = GameDataManager.Instance.InventorySystem;
        ItemData itemToDrop = inventorySystem.GetItemAt(sourceSlotUI.slotIndex);

        if (itemToDrop is EquipmentItemData equipmentItem && equipmentItem.slotType == this.slotType)
        {
            bool equipped = equipmentSystem.Equip(equipmentItem);
            if (equipped)
            {
                inventorySystem.RemoveItemAt(sourceSlotUI.slotIndex, 1);
            }
        }
    }
}