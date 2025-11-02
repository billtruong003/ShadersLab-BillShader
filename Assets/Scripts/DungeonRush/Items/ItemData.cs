using UnityEngine;

namespace DungeonRush.Items
{
    public abstract class ItemData : ScriptableObject
    {
        [Header("Common Info")]
        public string displayName;
        [TextArea(3, 5)] public string description;
        public Sprite icon;
        public bool isStackable = true;

        public abstract void Use(GameObject user);
    }
}