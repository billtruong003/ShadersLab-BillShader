using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Nebulanook.Core;

namespace Nebulanook.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class CharacterInteraction : MonoBehaviour
    {
        [Title("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private Vector3 offset = new Vector3(0, 1, 0);

        [Title("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField, ShowIf("showGizmos")] private Color gizmoColor = Color.yellow;

        private PlayerInputHandler inputHandler;
        private Collider[] hitColliders = new Collider[10];

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            if (inputHandler.InteractInputDown)
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position + offset, interactionRadius, hitColliders, interactableLayer);

            if (hits == 0) return;

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits; i++)
            {
                var interactable = hitColliders[i].GetComponent<IInteractable>();
                if (interactable == null) continue;

                float dist = Vector3.SqrMagnitude(hitColliders[i].transform.position - transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }

            closestInteractable?.Interact();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position + offset, interactionRadius);
        }
    }
}