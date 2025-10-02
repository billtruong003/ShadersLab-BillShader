// Path: Assets/Scripts/UI/EquipmentSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
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
}