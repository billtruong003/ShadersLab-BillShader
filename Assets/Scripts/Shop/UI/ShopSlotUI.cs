using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private Button purchaseButton;

    public void Initialize(ShopItem shopItem, Action onPurchase)
    {
        itemIcon.sprite = shopItem.item.icon;
        itemNameText.text = shopItem.item.itemName;
        itemPriceText.text = shopItem.purchasePrice.ToString() + " G";

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() => onPurchase?.Invoke());
    }
}