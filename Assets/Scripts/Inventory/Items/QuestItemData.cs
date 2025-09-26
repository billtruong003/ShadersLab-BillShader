using UnityEngine;

[CreateAssetMenu(fileName = "New Quest Item", menuName = "Inventory/Item/Quest Item")]
public class QuestItemData : ItemData
{
    public override void Use(GameObject user)
    {
        Debug.Log($"{itemName} is a quest item and cannot be used directly from the inventory.");
    }
}