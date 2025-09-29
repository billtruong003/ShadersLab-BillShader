using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopSystem shopSystem;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject shopSlotPrefab;
    [SerializeField] private Transform slotContainer;

    void Start()
    {
        gameObject.SetActive(false); // Ẩn UI lúc đầu
    }

    public void ToggleUI()
    {
        bool isActive = !gameObject.activeSelf;
        gameObject.SetActive(isActive);
        if (isActive)
        {
            Redraw();
        }
    }

    private void Redraw()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var shopItem in shopSystem.AvailableItems)
        {
            GameObject slotInstance = Instantiate(shopSlotPrefab, slotContainer);
            ShopSlotUI slotUI = slotInstance.GetComponent<ShopSlotUI>();
            slotUI.Initialize(shopItem, () =>
            {
                shopSystem.PurchaseItem(shopItem.item, playerInventory);
            });
        }
    }
}