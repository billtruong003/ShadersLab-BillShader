using Clicknext.StylizedLocalVillage.Entities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Templates
{
    public class ProcessTemplate : MonoBehaviour
    {
        public Action OnCraft = delegate { };
        public int CraftAmount { get; set; }
        public float DurationRemain { get; set; }

        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] Button craftButton;
        [SerializeField] Transform rawRoot;
        [SerializeField] RawTemplate rawTemplate;
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text queueText;

        [Header("Ingredients")]
        [SerializeField] TMP_Text requirementText;
        [SerializeField] Button requireButton;
        [SerializeField] Button closeBtn;
        [SerializeField] GameObject ingredientPanel;
        [SerializeField] Color validColor;
        [SerializeField] Color invalidColor;

        private Product mProduct;
        private Ingredient[] mRawData;

        void Awake()
        {
            craftButton.interactable = false;
            rawTemplate.gameObject.SetActive(false);
            craftButton.onClick.AddListener(() => OnCraft());
            requireButton.onClick.AddListener(() => ShowIngredients(true));
            closeBtn.onClick.AddListener(() => ShowIngredients(false));
        }

        private void ShowIngredients(bool value) =>
            ingredientPanel.SetActive(value);

        public void Set(Product product, Ingredient[] rawData, float iconSize)
        {
            mProduct = product;
            mRawData = rawData;

            icon.sprite = Resources.Load<Sprite>(product.Type.ToString());
            icon.GetComponent<RectTransform>().sizeDelta *= iconSize;
            nameText.text = product.Type.ToString();
            foreach (var each in rawData)
            {
                var raw = Instantiate(rawTemplate, rawRoot);
                    raw.Set(each.product.Type, each.amount);
            }

            DurationRemain = product.ProduceTime;
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            queueText.text = $"{CraftAmount}";
            timeText.text = $"{DurationRemain:0}";
            amountText.text = mProduct != null ? Storage.Get(mProduct.Type).ToString() : "0";

            var isValid = IsValid();
            requirementText.text = isValid ? "Meet the requirements" : "Missing Ingredients";
            requirementText.color = isValid ? validColor : invalidColor;
            craftButton.interactable = isValid;
        }

        public bool IsValid()
        {
            bool isValid = true;
            foreach (var each in mRawData)
            {
                if (each.amount > Storage.Get(each.product.Type))
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }
    }
}
