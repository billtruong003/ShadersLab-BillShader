using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class SlotTemplate : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text AmountText;
        [SerializeField] private TMP_Text NameText;

        public void SetAmount(int value)
        {
            AmountText.text = value.ToString();
            gameObject.SetActive(value > 0);
        }

        public void SetName(string value)
        {
            NameText.text = value;
            icon.sprite = Resources.Load<Sprite>(value);
        }
    }
}
