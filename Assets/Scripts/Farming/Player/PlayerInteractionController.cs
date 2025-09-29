using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ActiveToolSystem toolSystem;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionOffset = 1f;
    [SerializeField] private Vector3 interactionBoxHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private LayerMask interactableLayerMask;

    private IInteractable currentInteractable;
    private WorldSpacePromptUI currentPrompt; // Giữ tham chiếu đến UI của đối tượng hiện tại

    void Update()
    {
        FindInteractable();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(this.gameObject, toolSystem);
        }
    }

    private void FindInteractable()
    {
        Vector3 boxCenter = transform.position + transform.forward * interactionOffset;
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, interactionBoxHalfExtents, transform.rotation, interactableLayerMask);

        IInteractable closestInteractable = FindClosestInteractable(hitColliders, out WorldSpacePromptUI prompt);

        if (currentInteractable != closestInteractable)
        {
            // Tắt prompt cũ trước khi chuyển sang prompt mới
            currentPrompt?.HideInteractionPrompt();

            currentInteractable = closestInteractable;
            currentPrompt = prompt;
        }

        // Cập nhật prompt liên tục mỗi frame
        if (currentInteractable != null && currentPrompt != null)
        {
            string promptText = currentInteractable.GetInteractionPrompt(toolSystem);
            if (!string.IsNullOrEmpty(promptText))
            {
                currentPrompt.ShowInteractionPrompt(promptText);
            }
            else
            {
                currentPrompt.HideInteractionPrompt();
            }
        }
    }

    private IInteractable FindClosestInteractable(Collider[] colliders, out WorldSpacePromptUI foundPrompt)
    {
        float minDistanceSqr = float.MaxValue;
        IInteractable closest = null;
        foundPrompt = null;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<IInteractable>(out var interactable))
            {
                float distanceSqr = (transform.position - collider.transform.position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closest = interactable;
                    // Lấy luôn component UI trên đối tượng đó
                    collider.TryGetComponent<WorldSpacePromptUI>(out foundPrompt);
                }
            }
        }
        return closest;
    }

    // (Giữ lại Gizmos như cũ)
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 boxCenter = transform.position + transform.forward * interactionOffset;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, interactionBoxHalfExtents * 2);
    }
#endif
}