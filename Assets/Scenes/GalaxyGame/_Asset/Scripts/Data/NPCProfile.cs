using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Data/NPC Profile")]
public class NPCProfile : ScriptableObject
{
    [BoxGroup("Identity")]
    public string ID;
    [BoxGroup("Identity")]
    public GameObject Prefab;

    [BoxGroup("Dialogue Data")]
    public CharacterProfile CharacterInfo;
    [BoxGroup("Dialogue Data")]
    public DialogueConversation DefaultConversation;
}