using UnityEngine;
using Sirenix.OdinInspector;
using Nebulanook.Core;
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour, IInteractable
{
    [Title("Conversation Data")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [SerializeField] private DialogueConversation conversation;

    [Title("Debug Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField, ShowIf("showGizmos")]
    private Color gizmoColor = new Color(0f, 1f, 1f, 0.5f);

    [SerializeField, ShowIf("showGizmos")]
    [Range(0.1f, 2f)] private float gizmoSize = 0.5f;

    public void Interact()
    {
        if (conversation == null)
        {
            Debug.LogWarning($"Object {gameObject.name} Missing Conversation!", this);
            return;
        }

        DialogueManager.Instance.StartDialogue(conversation);
    }

    public void SetConversation(DialogueConversation convo)
    {
        this.conversation = convo;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.2f);
    }

    [InfoBox("Layer must be 'Interactable'!", InfoMessageType.Error, "IsLayerInvalid")]
    private bool IsLayerInvalid()
    {
        return LayerMask.LayerToName(gameObject.layer) != "Interactable";
    }
}