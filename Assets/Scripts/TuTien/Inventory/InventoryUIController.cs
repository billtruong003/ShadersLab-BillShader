using UnityEngine;
using VoTanTuTien.Inventory;

namespace VoTanTuTien.UI
{
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private InventorySlotUI slotPrefab;

        private InventoryManager inventoryManager;

        private void Start()
        {
            inventoryManager = InventoryManager.Instance;
            inventoryManager.OnInventoryChanged += Redraw;
            inventoryPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryChanged -= Redraw;
            }
        }

        public void TogglePanel()
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);
            if (isActive)
            {
                Redraw();
            }
        }

        private void Redraw()
        {
            foreach (Transform child in slotsParent)
            {
                Destroy(child.gameObject);
            }

            foreach (var item in inventoryManager.GetItems())
            {
                InventorySlotUI newSlot = Instantiate(slotPrefab, slotsParent);
                newSlot.Display(item);
            }
        }
    }
}