using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Item/Consumable")]
public class ConsumableItemData : ItemData
{
    public int healthToRestore;

    public override void Use(GameObject user)
    {
        Debug.Log($"Used {itemName}, restored {healthToRestore} health to {user.name}.");
        // Giả lập logic hồi máu. Trong dự án thực tế, bạn sẽ gọi đến một component HealthSystem trên user.
        // user.GetComponent<PlayerHealth>()?.RestoreHealth(healthToRestore);
    }
}