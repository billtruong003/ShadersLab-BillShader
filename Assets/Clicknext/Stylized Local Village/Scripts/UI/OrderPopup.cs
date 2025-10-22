using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI
{
    public class OrderPopup : MonoBehaviour
    {
        public event Action OnGatheredProduct = delegate { };

        [SerializeField] private GameObject canvas;
        [SerializeField] private Button pickButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private RectTransform rectDialog;
        [SerializeField] private Toggle changeToggle;
        [SerializeField] private Image selectedImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text timeText;

        [SerializeField] private GameObject namePanel;
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameObject upgradePanel;

        [SerializeField] private ProductTemplate productTemplate;
        [SerializeField] private SlotTemplate slotTemplate;

        private Vector3 _targetPosition;
        private Product[] _products;
        private Action<Product> _onSelected;

        private readonly bool[] _unlocked = new bool[Enum.GetValues(typeof(Item)).Length];

        private void Awake()
        {
            pickButton.onClick.AddListener(() => OnGatheredProduct());
            changeToggle.onValueChanged.AddListener(ShowSelection);
            Hide();
        }

        public void Visible(Vector3 position, Product currentProduct, Product[] products, Action<Product> onSelected)
        {
            if (!currentProduct)
                return;

            changeToggle.isOn = false;
            canvas.SetActive(true);
            ShowSelection(false);

            SelectProduct(currentProduct);

            _targetPosition = position; 
            _products = products;
            _onSelected = onSelected;
        }

        private IEnumerator LoadProducts(Product[] products, Action<Product> onSelected)
        {
            productTemplate.gameObject.SetActive(false);

            foreach (Transform each in productTemplate.transform.parent)
            {
                if (each.gameObject != productTemplate.gameObject)
                    Destroy(each.gameObject);
            }

            foreach (var product in products)
            {
                var isUnlocked = _unlocked[(int)product.Type] ? _unlocked[(int)product.Type]: !product.isLocked;
                var eachProduct = Instantiate(productTemplate, productTemplate.transform.parent);
                    eachProduct.SetLock(!isUnlocked);
                    eachProduct.Icon.sprite = Resources.Load<Sprite>(product.Type.ToString());
                    eachProduct.SelectButton.onClick.AddListener(() =>
                    {
                        if (isUnlocked)
                        {
                            SelectProduct(product);
                            onSelected(product);
                        }
                        else
                        {
                            StartCoroutine(LoadUpgradeProducts(product));
                            upgradeButton.onClick.RemoveAllListeners();
                            upgradeButton.onClick.AddListener(() =>
                            {
                                if (Upgrade(product))
                                {
                                    _unlocked[(int)product.Type] = true;
                                    eachProduct.SetLock(false);
                                    SelectProduct(product);
                                    onSelected(product);
                                }
                            });
                            upgradePanel.SetActive(true);
                        }
                    });
                    eachProduct.gameObject.SetActive(true);

                yield return new WaitForEndOfFrame();
            }
        }

        private bool Upgrade(Product product)
        {
            bool isEnough = true;
            foreach (var item in product.upgradeItems)
                if (Storage.Get(item.product.Type) < item.amount) 
                {
                    isEnough = false;
                    break;
                }

            if(isEnough)
                foreach(var item in product.upgradeItems)
                    Storage.Remove(item.product.Type, item.amount);

            return isEnough;
        }

        private IEnumerator LoadUpgradeProducts(Product product)
        {
            slotTemplate.gameObject.SetActive(false);

            foreach (Transform each in slotTemplate.transform.parent)
            {
                if (each.gameObject != slotTemplate.gameObject)
                    Destroy(each.gameObject);
            }

            foreach(var item in product.upgradeItems)
            {
                var slot = Instantiate(slotTemplate, slotTemplate.transform.parent);
                    slot.SetName(item.product.Type.ToString());
                    slot.SetAmount(item.amount);
                    slot.gameObject.SetActive(true);
                yield return new WaitForEndOfFrame();
            }
        }

        private void SelectProduct(Product product)
        {
            nameText.text = product.Type.ToString();
            selectedImage.sprite = Resources.Load<Sprite>(product.Type.ToString());
            changeToggle.isOn = false;
        }

        public void Hide() => canvas.SetActive(false);

        private void ShowSelection(bool value)
        {
            upgradePanel.SetActive(false);
            selectionPanel.SetActive(value);
            namePanel.SetActive(!value);

            if (value)
            {
                StopAllCoroutines();
                StartCoroutine(LoadProducts(_products, _onSelected));
            }
        }

        public void UpdateInformation(int amount, float producTime)
        {
            amountText.text = amount.ToString();
            timeText.text = $"{producTime + 1:0}s";
        }

        private void Update()
        {
            transform.position = Camera.main.WorldToScreenPoint(_targetPosition);
        }
    }
}
