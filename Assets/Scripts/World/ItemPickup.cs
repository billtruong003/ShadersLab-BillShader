using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;

    // Để tránh nhặt nhiều lần, ta sẽ disable collider sau khi nhặt
    private Collider itemCollider;

    private void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = true; // Đảm bảo nó là trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        // Giả định Player có component PlayerInventory
        var playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            bool success = playerInventory.AddItem(itemData, quantity);
            if (success)
            {
                // Tạm thời phá hủy object sau khi nhặt
                // Nâng cao: sử dụng Object Pool để trả về pool
                Destroy(gameObject);
            }
        }
    }
}