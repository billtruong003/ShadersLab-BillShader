using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class RewardTemplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject receievedHighligth;
        [SerializeField] private Image header;
        [SerializeField] private Image background;
        [SerializeField] private Color headerColor;
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color receivedHeaderColor;
        [SerializeField] private Color receivedBackgroundColor;

        private void Awake() =>
            SetReceived(false);

        public void Set(string level, string amount, string iconPath, float scale)
        {
            levelText.text = level;
            amountText.text = amount;
            icon.sprite = Resources.Load<Sprite>(iconPath);
            icon.GetComponent<RectTransform>().sizeDelta *= scale;
        }

        public void SetReceived(bool value)
        {
            header.color = value ? receivedHeaderColor : headerColor;
            background.color = value ? receivedBackgroundColor : backgroundColor;
            receievedHighligth.SetActive(value);
        }
    }
}
