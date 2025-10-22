using Clicknext.StylizedLocalVillage.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class RawTemplate : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text text;

        private Item mItem;
        private int mAmount;

        private void Start() => DisplayAmount();

        public void Set(Item item, int amount)
        {
            icon.sprite = Resources.Load<Sprite>(item.ToString());
            mAmount = amount;
            mItem = item;
            gameObject.SetActive(true);
        }

        private void FixedUpdate() => DisplayAmount();

        private void DisplayAmount() => text.text = $"{mAmount}/{Storage.Get(mItem)}";
    }
}
