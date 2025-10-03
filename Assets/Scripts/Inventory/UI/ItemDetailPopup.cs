using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailPopup : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    private ItemData currentItem;
    private int sourceSlotIndex;

    private void Awake()
    {
        actionButton.onClick.AddListener(OnActionButtonClicked);
        dropButton.onClick.AddListener(OnDropButtonClicked);
        closeButton.onClick.AddListener(Hide);
    }

    public void Display(InventorySlot slot, int index)
    {
        currentItem = slot.itemData;
        sourceSlotIndex = index;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        iconImage.sprite = currentItem.icon;
        nameText.text = currentItem.itemName;
        descriptionText.text = currentItem.description;

        ConfigureButtons();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentItem = null;
    }

    private void ConfigureButtons()
    {
        if (currentItem is EquipmentItemData || currentItem is CosmeticItemData)
        {
            actionButtonText.text = "Equip";
            actionButton.gameObject.SetActive(true);
        }
        else if (currentItem is ConsumableItemData)
        {
            actionButtonText.text = "Use";
            actionButton.gameObject.SetActive(true);
        }
        else
        {
            actionButton.gameObject.SetActive(false);
        }

        dropButton.gameObject.SetActive(true);
    }

    private void OnActionButtonClicked()
    {
        if (currentItem != null)
        {
            InventoryUIManager.Instance.RequestUseItem(currentItem, sourceSlotIndex);
        }
    }

    private void OnDropButtonClicked()
    {
        InventoryUIManager.Instance.RequestDropItem(sourceSlotIndex, true); // true = drop all in stack
    }
}