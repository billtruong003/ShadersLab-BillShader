using Clicknext.StylizedLocalVillage.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class FloatingTemplate : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject coin;
        [SerializeField] private GameObject bag;
        [SerializeField] private GameObject exp;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private Color positiveColor;
        [SerializeField] private Color negativeColor;

        public void Spawn(Item type, int value, Vector3 position)
        {
            icon.sprite = Resources.Load<Sprite>(type.ToString());
            icon.gameObject.SetActive(true);
            coin.SetActive(false);
            bag.SetActive(false);
            exp.SetActive(false);
            SetValue(value, position);
        }

        public void Spawn(Currency currency, int value, Vector3 position)
        {
            coin.SetActive(currency == Currency.Coin);
            bag.SetActive(currency == Currency.Bag);
            exp.SetActive(currency == Currency.EXP);
            icon.gameObject.SetActive(false);
            SetValue(value, position);
        }

        private void SetValue(int value, Vector3 position)
        {
            amount.text = value > 0 ? $"+{value}" : $"-{value}";
            amount.color = value > 0 ? positiveColor: negativeColor;
            transform.position = Camera.main.WorldToScreenPoint(position);
            gameObject.SetActive(true);
        }

        public void OnAnimationEnd() => Destroy(gameObject);
    }
}
