using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct ShopItem
{
    public ItemData item;
    public int purchasePrice;
}

public class ShopSystem : MonoBehaviour
{
    [SerializeField] private List<ShopItem> availableItems;
    // Tạm thời giả định người chơi có một component quản lý tiền tệ
    // [SerializeField] private PlayerCurrency playerCurrency;

    public IReadOnlyList<ShopItem> AvailableItems => availableItems;

    public bool PurchaseItem(ItemData item, PlayerInventory inventory)
    {
        ShopItem? shopItem = FindShopItem(item);
        if (shopItem == null)
        {
            Debug.LogWarning($"Item {item.itemName} is not available in this shop.");
            return false;
        }

        // int price = shopItem.Value.purchasePrice;
        // if (!playerCurrency.CanAfford(price))
        // {
        //     Debug.Log("Not enough money!");
        //     return false;
        // }

        bool addedToInventory = inventory.AddItem(item, 1);
        if (addedToInventory)
        {
            // playerCurrency.Spend(price);
            Debug.Log($"Purchased {item.itemName}.");
            return true;
        }

        Debug.Log("Inventory is full.");
        return false;
    }

    private ShopItem? FindShopItem(ItemData item)
    {
        foreach (var si in availableItems)
        {
            if (si.item == item)
            {
                return si;
            }
        }
        return null;
    }
}