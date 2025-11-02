using DungeonRush.Inventories;
using DungeonRush.Items;
using UnityEngine;

namespace DungeonRush
{
    [RequireComponent(typeof(DungeonRush.Inventories.InventorySystem))]
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayer;

        private DungeonRush.Inventories.InventorySystem playerInventory;
        private DungeonRush.Items.ItemPickup currentTarget;

        private void Awake()
        {
            playerInventory = GetComponent<DungeonRush.Inventories.InventorySystem>();
        }

        private void Update()
        {
            FindInteractable();
            HandleInteractionInput();
        }

        private void FindInteractable()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

            DungeonRush.Items.ItemPickup closestPickup = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.TryGetComponent<DungeonRush.Items.ItemPickup>(out var pickup))
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPickup = pickup;
                    }
                }
            }

            if (currentTarget != closestPickup)
            {
                currentTarget?.HidePrompt();
                currentTarget = closestPickup;
                currentTarget?.ShowPrompt();
            }
        }

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
            {
                if (currentTarget.PickupItem(playerInventory))
                {
                    // Vật phẩm đã được nhặt thành công, target sẽ tự hủy
                    // FindInteractable() ở frame tiếp theo sẽ tự dọn dẹp currentTarget
                }
                else
                {
                    // Có thể hiện thông báo "Inventory Full" ở đây
                    Debug.Log("Inventory is full!");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}