using DungeonRush.Inventories;
using UnityEngine;
using TMPro;

namespace DungeonRush.Items
{
    [RequireComponent(typeof(SphereCollider))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private DungeonRush.Items.ItemData itemData;
        [SerializeField] private int quantity = 1;

        [Header("UI Feedback")]
        [SerializeField] private TextMeshPro pickupPromptText;
        [SerializeField] private Vector3 promptOffset = new Vector3(0, 1.5f, 0);

        private Transform playerCameraTransform;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            if (pickupPromptText != null)
            {
                pickupPromptText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Tối ưu việc tìm camera
            if (Camera.main != null)
            {
                playerCameraTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            // UI prompt luôn hướng về phía camera
            if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf && playerCameraTransform != null)
            {
                pickupPromptText.transform.LookAt(playerCameraTransform);
            }
        }

        public bool PickupItem(DungeonRush.Inventories.InventorySystem inventory)
        {
            if (inventory.AddItem(itemData, quantity))
            {
                Destroy(gameObject);
                return true;
            }
            return false; // Không thể nhặt do hòm đồ đầy
        }

        public void ShowPrompt()
        {
            if (pickupPromptText != null)
            {
                pickupPromptText.text = $"Press [E] to pick up\n{itemData.displayName}";
                pickupPromptText.transform.position = transform.position + promptOffset;
                pickupPromptText.gameObject.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (pickupPromptText != null)
            {
                pickupPromptText.gameObject.SetActive(false);
            }
        }
    }
}