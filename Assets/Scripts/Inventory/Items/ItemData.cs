using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Information")]
    public string itemName = "New Item";
    [TextArea(4, 4)]
    public string description = "Item Description";
    public Sprite icon = null;
    public int maxStackSize = 1;

    public abstract void Use(GameObject user);
}