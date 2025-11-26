using UnityEngine;
using Sirenix.OdinInspector;

public enum CharacterType { Player, NPC }

[CreateAssetMenu(menuName = "Data/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [BoxGroup("Identity"), EnumToggleButtons, HideLabel]
    public CharacterType Type;

    [BoxGroup("Identity"), PreviewField(60, ObjectFieldAlignment.Left)]
    public Sprite Portrait;

    [BoxGroup("Identity"), VerticalGroup("Identity/Info")]
    public string CharacterName;

    [BoxGroup("Identity"), VerticalGroup("Identity/Info")]
    public Color NameColor = Color.white;

    [BoxGroup("Audio")]
    public AudioClip VoiceBlip;
}