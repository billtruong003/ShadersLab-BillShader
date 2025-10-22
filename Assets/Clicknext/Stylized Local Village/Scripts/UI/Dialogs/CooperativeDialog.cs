using Clicknext.StylizedLocalVillage.Characters;
using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{
    public class CooperativeDialog : MonoBehaviour
    {
        [Serializable] 
        private struct Data
        {
            public Product product;
            public float iconSize;
        }

        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text disadvantageText;
        [SerializeField] Button sellButton;

        [SerializeField] Color validColor;
        [SerializeField] Color invalidColor;
        [SerializeField] ToggleGroup toggleGroup;

        [SerializeField] GameObject dialog;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] MissionTemplate missionTemplate;
        [SerializeField] Data[] data;

        private Product mProduct;
        private int mAmount;
        private float mDisadvantage;

        void Awake()
        {
            dialog.SetActive(false);
            sellButton.interactable = false;
            missionTemplate.gameObject.SetActive(false);
            openButton.onClick.AddListener(() => dialog.SetActive(true));
            closeButton.onClick.AddListener(() => dialog.SetActive(false));
            sellButton.onClick.AddListener (Sell);

            foreach (var each in data)
            {
                var mission = Instantiate(missionTemplate, missionTemplate.transform.parent);
                    mission.Spawn(each.product, each.iconSize);
                    mission.OnSelected += OnSelected;
            }

            OnSelected(null, 0, 0f);
        }

        private void OnSelected(Product product, int amount, float disadvantage)
        {
            mProduct = product;
            mAmount = amount;
            mDisadvantage = disadvantage;
        }

        private void FixedUpdate()
        {
            if (!mProduct)
                return;

            var price = Market.GetPrice(mProduct.Type, mProduct.IngredientCount);
            nameText.text = mProduct ? mProduct.Type.ToString() : "???";
            priceText.text = mProduct ? price.ToString() : "0";
            countText.text = mProduct ? $"({mAmount})" : "(0)";

            var summary = mProduct ? price * mAmount : 0;
            var total = (int)(summary * mDisadvantage);
            var disadvantages = total - summary;

            totalText.text = mProduct ? total.ToString() : "0";
            disadvantageText.text = mProduct ? disadvantages.ToString() : "(-0)";

            if (mProduct == null)
                toggleGroup.SetAllTogglesOff();

            sellButton.interactable = mAmount <= Storage.Get(mProduct.Type);
        }

        private void Sell()
        {
            if (!mProduct)
                return;

            if (mAmount > Storage.Get(mProduct.Type))
                return;

            var summary = mProduct ? Market.GetPrice(mProduct.Type, mProduct.IngredientCount) * mAmount : 0;
            var total = (int)(summary * mDisadvantage);

            Storage.Remove(mProduct.Type, mAmount);
            Vault.Coin += total;

            var truck = FindAnyObjectByType<Truck>();
                truck.Transport();
        }

    }
}
