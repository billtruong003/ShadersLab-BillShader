using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoTanTuTien.Items;
using Sirenix.OdinInspector;

namespace VoTanTuTien.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Required]
        [SerializeField] private Image iconImage;
        [Required]
        [SerializeField] private TextMeshProUGUI stackSizeText;

        public void Display(InventoryItem item)
        {
            iconImage.sprite = item.data.icon;
            iconImage.enabled = true;

            if (item.data.isStackable && item.stackSize > 1)
            {
                stackSizeText.text = item.stackSize.ToString();
                stackSizeText.enabled = true;
            }
            else
            {
                stackSizeText.enabled = false;
            }
        }
    }
}