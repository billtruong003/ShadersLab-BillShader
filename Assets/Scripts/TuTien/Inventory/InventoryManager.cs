using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoTanTuTien.Items;
using System;

namespace VoTanTuTien.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public event Action OnInventoryChanged;

        [SerializeField] private int inventorySize = 20;
        private List<InventoryItem> items = new List<InventoryItem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public bool AddItem(VoTanTuTien.Items.ItemData itemData)
        {
            if (itemData.isStackable)
            {
                InventoryItem existingItem = items.FirstOrDefault(item => item.data == itemData && item.stackSize < itemData.maxStackSize);
                if (existingItem != null)
                {
                    existingItem.AddToStack();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            if (items.Count < inventorySize)
            {
                items.Add(new InventoryItem(itemData));
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false; // Inventory is full
        }

        public List<InventoryItem> GetItems()
        {
            return items;
        }
    }
}