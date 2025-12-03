using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(DialogueTrigger))]
[RequireComponent(typeof(DialogueActorWorld))]
public class NPCController : MonoBehaviour
{
    [SerializeField] private DialogueTrigger _dialogueTrigger;
    [SerializeField] private DialogueActorWorld _actorWorld;

    private NPCProfile _currentProfile;

    public void Initialize(NPCProfile profile)
    {
        _currentProfile = profile;

        if (_dialogueTrigger != null && profile.DefaultConversation != null)
        {
            _dialogueTrigger.SetConversation(profile.DefaultConversation);
        }

        if (_actorWorld != null && profile.CharacterInfo != null)
        {
            var field = typeof(DialogueActorWorld).GetField("profile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(_actorWorld, profile.CharacterInfo);
            }

            _actorWorld.enabled = false;
            _actorWorld.enabled = true;
        }
    }

    private void OnValidate()
    {
        if (_dialogueTrigger == null) _dialogueTrigger = GetComponent<DialogueTrigger>();
        if (_actorWorld == null) _actorWorld = GetComponent<DialogueActorWorld>();
    }
}