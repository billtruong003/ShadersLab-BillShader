using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using Clicknext.StylizedLocalVillage.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{
    public class MarketDialog : MonoBehaviour
    {
        [Serializable]
        private struct Data
        {
            public Toggle toggle;
            public Transform canvas;
            public Product[] products;
        }

        [SerializeField] GameObject dialog;
        [SerializeField] GoodsTemplate goodsTemplate;

        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button decreaseButton;
        [SerializeField] Button increaseButton;
        [SerializeField] Button buyButton;

        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text totalText;

        [SerializeField] Color validColor;
        [SerializeField] Color invalidColor;
        [SerializeField] CanvasGroup amountCanvas;
        [SerializeField] ToggleGroup toggleGroup;
        [SerializeField] Data[] data;

        private Product mProduct;
        private int mCount;

        private void Awake()
        {
            dialog.SetActive(false);
            increaseButton.onClick.AddListener(() => ApplyCount(++mCount));
            decreaseButton.onClick.AddListener(() => ApplyCount(--mCount));
            buyButton.onClick.AddListener(OnBought);
            closeButton.onClick.AddListener(() => dialog.SetActive(false));
            openButton.onClick.AddListener(() => dialog.SetActive(true));
            goodsTemplate.gameObject.SetActive(false);

            foreach (var each in data)
            {
                foreach (var product in each.products) 
                {
                    var goods = Instantiate(goodsTemplate, each.canvas);
                        goods.Spawn(product);
                        goods.OnSelected += OnSelected;
                }

                each.toggle.onValueChanged.AddListener(ison =>
                {
                    OnSelected(null);
                    each.canvas.gameObject.SetActive(ison);
                });
                each.canvas.gameObject.SetActive(false);
            }

            OnSelected(null);
            data[0].canvas.gameObject.SetActive(true);

            Timer.OnChanged += OnDayChanged;
        }

        private void OnDayChanged()
        {
            Market.ReGenerate();
            OnSelected(null);
            toggleGroup.SetAllTogglesOff();
        }

        private void OnSelected(Product product)
        {
            mProduct = product;
            nameText.text = product? mProduct.Type.ToString(): "???";
            priceText.text = product ? Market.GetPrice(mProduct.Type, mProduct.IngredientCount).ToString(): "0";
            totalText.text = "0";
            mCount = 1;
            countText.text = mCount.ToString();
            buyButton.gameObject.SetActive(product != null);
            priceText.gameObject.SetActive(product != null);

            amountCanvas.alpha = product ? 1 : 0.25f;
            amountCanvas.blocksRaycasts = product != null;

            if(product == null)
                toggleGroup.SetAllTogglesOff();

            UpdateRow();
        }

        private void FixedUpdate() => UpdateRow();

        private void UpdateRow()
        {
            timeText.text = Timer.TimeStamp;

            if (mProduct == null)
                return;

            countText.text = mCount.ToString();

            var totalPrice = (mCount * Market.GetPrice(mProduct.Type, mProduct.IngredientCount));
            totalText.text = totalPrice.ToString();
            priceText.color = totalText.color = Vault.Coin >= totalPrice ? validColor : invalidColor;
            buyButton.interactable = Vault.Coin >= totalPrice && mCount > 0;
        }


        private void ApplyCount(int count)
        {
            if (!mProduct)
                return;

            count = Mathf.Clamp(count, 1, Market.GetAmount(mProduct.Type));
            mCount = count;
        }

        private void OnBought()
        {
            var count = Market.Remove(mProduct.Type, mCount);
            Storage.Add(mProduct.Type, count);
            Vault.Coin -= count * Market.GetPrice(mProduct.Type, mProduct.IngredientCount);
            if (Market.GetAmount(mProduct.Type) <= 0)
                OnSelected(null);
        }
    }
}
