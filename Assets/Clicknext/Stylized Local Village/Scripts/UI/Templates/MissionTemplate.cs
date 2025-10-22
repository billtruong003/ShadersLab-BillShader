using Clicknext.StylizedLocalVillage.Entities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class MissionTemplate : MonoBehaviour
    {
        public event Action<Product, int, float> OnSelected = delegate { };

        [SerializeField] Image icon;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] Color validColor;
        [SerializeField] Color invalidColor;
        [SerializeField] CanvasGroup canvas;
        [SerializeField] Toggle selectToggle;

        [SerializeField] int minAmount;
        [SerializeField] int maxAmount;

        private Product mProduct;
        private int mAmount;
        private float mDisadvantage;

        private void Awake()
        {
            selectToggle.onValueChanged.AddListener(ison =>
            {
                if (ison)
                    OnSelected(mProduct, mAmount, mDisadvantage);

                selectToggle.interactable = !ison;
            });
        }

        public void Spawn(Product product, float iconSize)
        {
            mProduct = product;
            icon.sprite = Resources.Load<Sprite>(product.Type.ToString());
            icon.GetComponent<RectTransform>().sizeDelta *= iconSize;
            gameObject.SetActive(true);
            GenerateOrder();
        }

        private void OnEnable() => UpdateRow();
        private void FixedUpdate() => UpdateRow();

        private void UpdateRow()
        {
            if (mProduct == null)
                return;

            var price = Market.GetPrice(mProduct.Type, mProduct.IngredientCount);
            priceText.text = price.ToString();
            amountText.color = mAmount > Storage.Get(mProduct.Type) ? invalidColor : validColor;
        }

        private void GenerateOrder()
        {
            mAmount = UnityEngine.Random.Range(minAmount, maxAmount);
            mDisadvantage = UnityEngine.Random.Range(0f, 1f);
            amountText.text = mAmount.ToString();
        }
    }
}
