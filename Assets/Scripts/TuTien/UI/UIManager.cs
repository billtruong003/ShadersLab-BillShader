using UnityEngine;

namespace VoTanTuTien.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public StatsUIController StatsUI { get; private set; }
        public InventoryUIController InventoryUI { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            StatsUI = GetComponentInChildren<StatsUIController>(true);
            InventoryUI = GetComponentInChildren<InventoryUIController>(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                InventoryUI?.TogglePanel();
            }
        }
    }
}