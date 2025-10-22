using Clicknext.StylizedLocalVillage.Entities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class GoodsTemplate : MonoBehaviour
    {
        public event Action<Product> OnSelected = delegate { };

        [SerializeField] Image icon;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] Color validColor;
        [SerializeField] Color invalidColor;
        [SerializeField] CanvasGroup canvas;
        [SerializeField] Toggle selectToggle;

        private Product mProduct;

        private void Awake()
        {
            selectToggle.onValueChanged.AddListener(ison =>
            {
                if (ison)
                    OnSelected(mProduct);

                selectToggle.interactable = !ison;
            });
        }

        public void Spawn(Product product)
        {
            mProduct = product;
            icon.sprite = Resources.Load<Sprite>(product.Type.ToString());
            gameObject.SetActive(true);
        }

        private void OnEnable() => UpdateRow();
        private void FixedUpdate() => UpdateRow();

        private void UpdateRow()
        {
            if (mProduct == null)
                return;

            var amount = Market.GetAmount(mProduct.Type);
            var price = Market.GetPrice(mProduct.Type, mProduct.IngredientCount);
            priceText.text = price.ToString();
            priceText.color = price > Vault.Coin ? invalidColor : validColor;
            amountText.text = amount.ToString();
            amountText.transform.parent.gameObject.SetActive(amount > 0);
            canvas.blocksRaycasts = amount > 0;
            canvas.alpha = amount > 0 ? 1f : 0.35f;
        }
    }
}
