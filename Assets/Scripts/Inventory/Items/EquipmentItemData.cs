using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Item/Equipment")]
public class EquipmentItemData : ItemData
{
    public float attackBonus;
    public float defenseBonus;

    public override void Use(GameObject user)
    {
        Debug.Log($"Equipped {itemName}. ATK+{attackBonus}, DEF+{defenseBonus} on {user.name}.");
        // Giả lập logic trang bị. Trong dự án thực tế, bạn sẽ gọi đến một component EquipmentManager trên user.
        // user.GetComponent<EquipmentManager>()?.Equip(this);
    }
}