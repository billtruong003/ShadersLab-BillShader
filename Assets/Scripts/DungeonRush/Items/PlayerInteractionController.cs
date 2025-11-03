using DungeonRush.Inventories;
using DungeonRush.Items;
using UnityEngine;

namespace DungeonRush
{
    [RequireComponent(typeof(InventorySystem))]
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayer;

        private DungeonRush.Inventories.InventorySystem playerInventory;
        private DungeonRush.Items.ItemPickup currentTarget;

        private void Awake()
        {
            playerInventory = GetComponent<Inventories.InventorySystem>();
        }

        private void Update()
        {
            FindBestInteractable();
            ProcessInteractionInput();
        }

        private void FindBestInteractable()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

            Items.ItemPickup closestPickup = null;
            float minDistanceSqr = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.TryGetComponent<Items.ItemPickup>(out var pickup))
                {
                    float distanceSqr = (transform.position - col.transform.position).sqrMagnitude;
                    if (distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                        closestPickup = pickup;
                    }
                }
            }

            UpdateTarget(closestPickup);
        }

        private void UpdateTarget(Items.ItemPickup newTarget)
        {
            if (currentTarget == newTarget) return;

            currentTarget?.HidePrompt();
            currentTarget = newTarget;
            currentTarget?.ShowPrompt();
        }

        private void ProcessInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
            {
                bool pickupSuccessful = currentTarget.PickupItem(playerInventory);

                if (pickupSuccessful)
                {
                    // Quan trọng: Dọn dẹp target ngay lập tức sau khi nhặt thành công.
                    // Điều này ngăn FindBestInteractable() tìm lại nó trong cùng một frame.
                    UpdateTarget(null);
                }
                else
                {
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