using UnityEngine;
using Sirenix.OdinInspector;

public class DialogueActorWorld : MonoBehaviour
{
    [Title("Identity")]
    [Required]
    [InlineEditor]
    [SerializeField] private CharacterProfile profile;

    [Title("Settings")]
    [SerializeField] private Transform speechPoint;

    private void Awake()
    {
        if (speechPoint == null) speechPoint = transform;
    }

    private void OnEnable()
    {
        DialogueActorRegistry.Register(profile, speechPoint);
    }

    private void OnDisable()
    {
        DialogueActorRegistry.Unregister(profile);
    }

    private void OnDrawGizmosSelected()
    {
        if (speechPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(speechPoint.position, 0.2f);
            Gizmos.DrawLine(speechPoint.position, speechPoint.position + Vector3.up * 0.5f);
        }
    }
}