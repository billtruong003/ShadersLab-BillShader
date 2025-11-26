using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour, IInteractable
{
    [Title("Conversation Data")]
    [Required("Bắt buộc phải gán Conversation!")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)] // Cho phép edit trực tiếp SO tại đây
    [SerializeField] private DialogueConversation conversation;

    [Title("Debug Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField, ShowIf("showGizmos")]
    private Color gizmoColor = new Color(0f, 1f, 1f, 0.5f); // Màu Cyan nhạt

    [SerializeField, ShowIf("showGizmos")]
    [Range(0.1f, 2f)] private float gizmoSize = 0.5f;

    // Hàm được gọi bởi CharacterInteract
    public void Interact()
    {
        if (conversation == null)
        {
            Debug.LogWarning($"Object {gameObject.name} bị thiếu DialogueConversation!", this);
            return;
        }

        DialogueManager.Instance.StartDialogue(conversation);

        // LookAtPlayer(); 
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;

        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.2f);
    }

    [InfoBox("Object này chưa được set Layer 'Interactable'. Player sẽ không thể tương tác!", InfoMessageType.Error, "IsLayerInvalid")]
    private bool IsLayerInvalid()
    {
        return LayerMask.LayerToName(gameObject.layer) != "Interactable";
    }
}

public interface IInteractable
{
    void Interact();
}