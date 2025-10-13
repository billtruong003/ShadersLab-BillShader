using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.Items
{
    [CreateAssetMenu(fileName = "New Item", menuName = "VoTanTuTien/Items/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Title("Item Information")]
        [PreviewField(75, ObjectFieldAlignment.Left)]
        public Sprite icon;
        public string itemName;
        [TextArea]
        public string description;
        public bool isStackable = true;
        public int maxStackSize = 99;
    }
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData data;
        public int stackSize;

        public InventoryItem(VoTanTuTien.Items.ItemData source)
        {
            data = source;
            stackSize = 1;
        }

        public void AddToStack()
        {
            stackSize++;
        }

    }
}